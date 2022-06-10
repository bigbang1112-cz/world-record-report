using BigBang1112.WorldRecordReportLib.Exceptions;
using BigBang1112.WorldRecordReportLib.Models;
using Humanizer.Bytes;
using Microsoft.Extensions.Caching.Memory;
using TmEssentials;
using TmXmlRpc;
using TmXmlRpc.Requests;
using Microsoft.Extensions.Logging;

using Game = BigBang1112.WorldRecordReportLib.Enums.Game;
using BigBang1112.WorldRecordReportLib.Models.ReportScopes;
using System.Runtime.CompilerServices;
using BigBang1112.WorldRecordReportLib.Enums;

namespace BigBang1112.WorldRecordReportLib.Services;

public class RefreshTM2Service : RefreshService
{
    private static readonly MasterServerTm2 server = new();
    
    private const string ScopeOfficialWR = $"{nameof(ReportScopeSet.TM2)}:{nameof(ReportScopeTM2.Nadeo)}:{nameof(ReportScopeTM2Nadeo.WR)}";
    private const string ScopeOfficialTop10 = $"{nameof(ReportScopeSet.TM2)}:{nameof(ReportScopeTM2.Nadeo)}:{nameof(ReportScopeTM2Nadeo.Changes)}";

    private readonly ILogger<RefreshTM2Service> _logger;
    private readonly IWrUnitOfWork _wrUnitOfWork;
    private readonly RefreshScheduleService _refreshScheduleService;
    private readonly RecordStorageService _recordStorageService;
    private readonly SnapshotStorageService _snapshotStorageService;
    private readonly IGhostService _ghostService;
    private readonly HttpClient _http;
    private readonly ReportService _reportService;

    public RefreshTM2Service(
        ILogger<RefreshTM2Service> logger,
        IWrUnitOfWork wrUnitOfWork,
        RefreshScheduleService refreshScheduleService,
        RecordStorageService recordStorageService,
        SnapshotStorageService snapshotStorageService,
        IGhostService ghostService,
        HttpClient http,
        ReportService reportService) : base(logger)
    {
        _logger = logger;
        _wrUnitOfWork = wrUnitOfWork;
        _refreshScheduleService = refreshScheduleService;
        _recordStorageService = recordStorageService;
        _snapshotStorageService = snapshotStorageService;
        _ghostService = ghostService;
        _http = http;
        _reportService = reportService;
    }

    /// <exception cref="RefreshLoopNotFoundException"/>
    /// <exception cref="MapGroupNotFoundException"/>
    public async Task RefreshOfficialAsync(CancellationToken cancellationToken = default)
    {
        if (_refreshScheduleService.TM2OfficialMapGroupCycle is null)
        {
            var mapGroups = await _wrUnitOfWork.MapGroups.GetAllOrderedAsync(cancellationToken);

            _refreshScheduleService.SetupTM2Official(mapGroups);
        }

        var mapGroup = _refreshScheduleService.NextTM2OfficialMapGroup();

        if (mapGroup is null)
        {
            return;
        }

        SetTitlePack(mapGroup);

        _logger.LogInformation("Fetching {count} maps [{mapGroup}]", mapGroup.Maps.Count, mapGroup);

        // Do not use mapGroup.Maps for updating or reading further map info! Take from wrHistories instead
        var mapUids = mapGroup.Maps.Select(x => x.MapUid);

        var leaderboardsTask = GetLeaderboardsFromMapsAsync(GetMapsForRequest(mapUids));

        var wrHistories = await GetWorldRecordHistoriesByMapGroupAsync(mapGroup);
        var mapDictionary = wrHistories.ToDictionary(x => x.Key, x => x.Value.Key);

        GetMapLeaderBoardSummaries<RequestGameManiaPlanet>.Response leaderboards;

        try
        {
            leaderboards = await leaderboardsTask;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning("Requesting TM2 solo leaderboards timed out: {msg}", ex.Message);
            return;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError("HTTP request exception when requesting TM2 solo leaderboards in RefreshTM2Service: {msg} (status code: {code})", ex.Message, ex.StatusCode);
            return;
        }

        var executionTime = leaderboards.ExecutionTime.TotalSeconds;
        var sizeStr = leaderboards.ByteSize.HasValue ? ByteSize.FromBytes(leaderboards.ByteSize.Value).ToString() : "unknown size";

        _logger.LogInformation("Finished in {executionTime}s ({sizeStr})", executionTime, sizeStr);

        foreach (var map in wrHistories.Values.Select(x => x.Key))
        {
            UpdateLastRefreshedOn(map);
        }

        var changes = new Dictionary<string, RecordChangesTM2>();

        var loginModels = await PopulateLoginModelsAsync(leaderboards, cancellationToken);

        foreach (var leaderboard in leaderboards) // TODO: run in parallel
        {
            var mapUid = leaderboard.MapUid;

            var change = await UpdateLeaderboardAsync(leaderboard, loginModels, wrHistories[mapUid], cancellationToken);

            if (change is not null)
            {
                changes.Add(mapUid, change);
            }

            // also not runnable in parallel
            await VerifyUnverifiedRecordsAsync(leaderboard, wrHistories[mapUid].Where(x => x.Unverified));
        }

        var worldRecords = changes.Values
            .Select(x => x.WorldRecordChange?.WorldRecord)
            .OfType<WorldRecordModel>();

        var recordChanges = changes.Values
            .SelectMany(x => x.RecordChanges ?? Enumerable.Empty<RecordSetDetailedChangeModel>());

        var recordCounts = changes.Values
            .Select(x => x.NewRecordCount)
            .OfType<RecordCountModel>();

        await _wrUnitOfWork.WorldRecords.AddRangeAsync(worldRecords, cancellationToken);
        await _wrUnitOfWork.RecordSetDetailedChanges.AddRangeAsync(recordChanges, cancellationToken);
        await _wrUnitOfWork.RecordCounts.AddRangeAsync(recordCounts, cancellationToken);

        await ReportChangesAsync(changes, mapDictionary);

        await _wrUnitOfWork.SaveAsync(cancellationToken);
    }

    private async Task ReportChangesAsync(Dictionary<string, RecordChangesTM2> changes, Dictionary<string, MapModel> mapDictionary)
    {
        var titlePackId = server.Game.Title;

        foreach (var (mapUid, change) in changes)
        {
            if (change.WorldRecordChange is not null)
            {
                if (change.WorldRecordChange.WorldRecord is null)
                {
                    foreach (var removedWr in change.WorldRecordChange.RemovedWorldRecords)
                    {
                        await _reportService.RemoveWorldRecordReportAsync(removedWr);
                    }
                }
                else if (change.WorldRecordChange.RemovedWorldRecords.Count > 0)
                {
                    await _reportService.ReportRemovedWorldRecordsAsync(
                        change.WorldRecordChange.WorldRecord,
                        change.WorldRecordChange.RemovedWorldRecords,
                        $"{ScopeOfficialWR}:{titlePackId}");
                }
                else
                {
                    await _reportService.ReportWorldRecordAsync(change.WorldRecordChange.WorldRecord, $"{ScopeOfficialWR}:{titlePackId}");
                }
            }

            if (change.LeaderboardChanges is not null && mapDictionary.TryGetValue(mapUid, out MapModel? map))
            {
                await _reportService.ReportDifferencesAsync(change.LeaderboardChanges, map, $"{ScopeOfficialTop10}:{titlePackId}");
            }
        }
    }

    private async Task<Dictionary<string, LoginModel>> PopulateLoginModelsAsync(IEnumerable<MapLeaderBoard> leaderboards, CancellationToken cancellationToken = default)
    {
        var loginNicknameDictionary = leaderboards
            .SelectMany(x => x.Records)
            .DistinctBy(x => x.Login)
            .ToDictionary(x => x.Login, x => x.Nickname);

        var loginModels = await _wrUnitOfWork.Logins.GetOrAddByNamesAsync(Game.TM2, loginNicknameDictionary, cancellationToken);

        foreach (var (login, model) in loginModels)
        {
            if (!loginNicknameDictionary.TryGetValue(login, out string? nickname))
            {
                continue;
            }

            if (model.Nickname is null)
            {
                model.Nickname = nickname;
                continue;
            }

            if (string.Equals(model.Nickname, nickname))
            {
                continue;
            }
            
            var latestNicknameChange = await _wrUnitOfWork.NicknameChanges.GetLatestByLoginAsync(model, cancellationToken);

            if (latestNicknameChange is not null
                && DateTime.UtcNow - latestNicknameChange.PreviousLastSeenOn <= TimeSpan.FromHours(1))
            {
                continue; // loginModel.Nickname set MUST be skipped, otherwise the change would get lost
            }

            // Track nickname change
            var nicknameChangeModel = new NicknameChangeModel
            {
                Login = model,
                Previous = model.Nickname,
                PreviousLastSeenOn = DateTime.UtcNow
            };

            await _wrUnitOfWork.NicknameChanges.AddAsync(nicknameChangeModel, cancellationToken);

            model.Nickname = nickname;
        }

        return loginModels;
    }

    private static void SetTitlePack(MapGroupModel mapGroup)
    {
        var titlePack = mapGroup.TitlePack;

        if (titlePack is null)
        {
            throw new Exception("Title pack definition is missing and required for this refresh loop.");
        }

        server.Game.Title = titlePack.GetTitleUid();
    }

    private static IList<GetMapLeaderBoardSummaries<RequestGameManiaPlanet>.Map>
        GetMapsForRequest(IEnumerable<string> mapUids)
    {
        return mapUids
            .Select(x => new GetMapLeaderBoardSummaries<RequestGameManiaPlanet>.Map(x))
            .ToList();
    }

    private static async Task<GetMapLeaderBoardSummaries<RequestGameManiaPlanet>.Response>
        GetLeaderboardsFromMapsAsync(IList<GetMapLeaderBoardSummaries<RequestGameManiaPlanet>.Map> maps)
    {
        try
        {
            MasterServer.Client.DefaultRequestHeaders.Date = DateTime.UtcNow; //
            return await server.GetMapLeaderBoardSummariesAsync(maps);
        }
        catch
        {
            throw;
        }
        finally
        {
            MasterServer.Client.DefaultRequestHeaders.Date = null;
        }
    }

    private async Task VerifyUnverifiedRecordsAsync(MapLeaderBoard leaderboard, IEnumerable<WorldRecordModel> unverifiedRecords)
    {
        foreach (var unverifiedWr in unverifiedRecords)
        {
            if (unverifiedWr.Player is null)
            {
                continue;
            }

            var verified = false;
            var replayUrl = default(string);
            var timestamp = default(DateTimeOffset);
            var login = default(string);
            var nickname = default(string);

            foreach (var record in leaderboard.Records)
            {
                if (record.Login == unverifiedWr.Player.Name
                 && record.Time == unverifiedWr.TimeInt32)
                {
                    verified = true;
                    replayUrl = record.ReplayUrl;
                    timestamp = (await _ghostService.DownloadGhostAndGetTimestampAsync(leaderboard.MapUid, record)) ?? unverifiedWr.PublishedOn;
                    login = record.Login;
                    nickname = record.Nickname;

                    break;
                }
            }

            if (!verified)
            {
                continue; // Further checking with about 2 hour tolerance
            }
            
            unverifiedWr.ReplayUrl = replayUrl;
            unverifiedWr.Unverified = false;
            unverifiedWr.DrivenOn = timestamp.UtcDateTime;
            unverifiedWr.PublishedOn = timestamp.UtcDateTime;
            unverifiedWr.Player.Name = login!;
            unverifiedWr.Player.Nickname = nickname;

            var report = await _wrUnitOfWork.Reports.GetByWorldRecordAsync(unverifiedWr);

            if (report is null)
            {
                return;
            }

            await _reportService.UpdateWorldRecordReportAsync(report);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="lb">Leaderboard to save to record storage.</param>
    private async Task<RecordChangesTM2?> UpdateLeaderboardAsync(
        MapLeaderBoard leaderboardFromApi,
        Dictionary<string, LoginModel> loginModels,
        IGrouping<MapModel, WorldRecordModel> wrHistory,
        CancellationToken cancellationToken = default)
    {
        var mapUid = leaderboardFromApi.MapUid;
        var zone = leaderboardFromApi.Zone;
        var map = wrHistory.Key;

        var top10Recs = leaderboardFromApi.Records
            .Select(x => new TM2Record(x.Rank, x.Login, x.Time, x.Nickname, x.ReplayUrl));

        // Scary to run in parallel


        var timestamps = await DownloadMissingGhostsAsync(mapUid, top10Recs, cancellationToken)
            .ToDictionaryAsync(x => x.Item1, x => x.Item2, cancellationToken);

        top10Recs = top10Recs.Select(x => timestamps.TryGetValue(x, out var timestamp) ? x with { Timestamp = timestamp } : x);

        var times = leaderboardFromApi.Times
            .Select(x => new UniqueRecord(x.time != -1 ? new TimeInt32(x.time) : null, x.count));

        var currentLeaderboard = new LeaderboardTM2(top10Recs, times);

        var leaderboardExists = _recordStorageService.OfficialLeaderboardExists(Game.TM2, mapUid, zone);

        if (!leaderboardExists)
        {
            currentLeaderboard = currentLeaderboard with
            {
                Records = await PopulateCurrentLeaderboardWithTimestamps(currentLeaderboard, timestamps).ToListAsync(cancellationToken)
            };

            await _recordStorageService.SaveTM2LeaderboardAsync(currentLeaderboard, mapUid, zone, cancellationToken: cancellationToken);
        }

        var previousLeaderboard = leaderboardExists
            ? await _recordStorageService.GetTM2LeaderboardAsync(mapUid, zone, cancellationToken: cancellationToken)
            : currentLeaderboard;

        if (previousLeaderboard is null)
        {
            throw new Exception("previousLeaderboard is null even though the leaderboard file exists");
        }

        var guaranteedNoDifferenceInTop10 = currentLeaderboard == previousLeaderboard;

        if (guaranteedNoDifferenceInTop10)
        {
            // Only resolve WR in the database
            // Not usable in paralel
            return await CheckJustWorldRecordAsync(map, currentLeaderboard.Records, loginModels, isFromManialink: false, cancellationToken);
        }

        currentLeaderboard = currentLeaderboard with
        {
            Records = await PopulateCurrentLeaderboardWithTimestamps(currentLeaderboard, timestamps, previousLeaderboard).ToListAsync(cancellationToken)
        };

        var diff = LeaderboardComparer.Compare(currentLeaderboard.Records, previousLeaderboard.Records);
        var diffTimes = LeaderboardComparer.CompareTimes(currentLeaderboard.Times, previousLeaderboard.Times);

        var recordChanges = default(IEnumerable<RecordSetDetailedChangeModel>);
        var leaderboardChanges = default(LeaderboardChangesRich<string>);

        if (diff is not null || diffTimes is not null)
        {
            if (diffTimes is not null)
            {
                map.LastActivityOn = DateTime.UtcNow;
            }

            var timestamp = _recordStorageService.GetOfficialLeaderboardLastUpdatedOn(Game.TM2, mapUid, zone);

            await _recordStorageService.SaveTM2LeaderboardAsync(currentLeaderboard, mapUid, zone, cancellationToken: cancellationToken);
        
            if (diff is not null)
            {
                if (timestamp.HasValue)
                {
                    await _snapshotStorageService.SaveTM2LeaderboardAsync(previousLeaderboard.Records, timestamp.Value, mapUid, zone, cancellationToken: cancellationToken);
                }
                
                var goneLoginModels = await _wrUnitOfWork.Logins.GetByNamesAsync(Game.TM2, diff.PushedOffRecords.Concat(diff.RemovedRecords).Select(x => x.PlayerId), cancellationToken);

                foreach (var (playerId, loginModel) in goneLoginModels)
                {
                    if (!loginModels.ContainsKey(playerId))
                    {
                        loginModels[playerId] = loginModel;
                    }
                }

                recordChanges = CreateListOfRecordChanges(diff, map, loginModels);

                leaderboardChanges = CreateLeaderboardChangesRich(diff,
                    currentLeaderboard.Records.ToDictionary(x => x.Login, x => (IRecord<string>)x),
                    previousLeaderboard.Records.ToDictionary(x => x.Login, x => (IRecord<string>)x));
            }
        }

        // Not usable in paralel
        var wrChange = await CheckWorldRecordAsync(map, currentLeaderboard.Records, loginModels, isFromManialink: false, cancellationToken);

        var newCount = default(RecordCountModel);

        var currentRecordCount = currentLeaderboard.GetRecordCount();
        var previousRecordCount = previousLeaderboard.GetRecordCount();
        
        if (currentRecordCount != previousRecordCount)
        {
            newCount = new RecordCountModel
            {
                Before = DateTime.UtcNow,
                Map = map,
                Count = currentRecordCount
            };
        }

        return new RecordChangesTM2(wrChange, newCount, recordChanges, leaderboardChanges);
    }

    private async IAsyncEnumerable<TM2Record> PopulateCurrentLeaderboardWithTimestamps(
        LeaderboardTM2 currentLeaderboard,
        Dictionary<TM2Record, DateTimeOffset?> timestamps,
        LeaderboardTM2? previousLeaderboard = null)
    {
        foreach (var rec in currentLeaderboard.Records)
        {
            var prevRec = previousLeaderboard?.Records.FirstOrDefault(x =>
                x.Time == rec.Time
             && string.Equals(x.Login, rec.Login)
             && string.Equals(x.ReplayUrl, rec.ReplayUrl));

            if (prevRec?.Timestamp.HasValue == true)
            {
                yield return rec with { Timestamp = prevRec.Timestamp.Value };
            }
            else if (rec.Timestamp is not null || rec.ReplayUrl is null || timestamps.ContainsKey(rec))
            {
                yield return rec;
            }
            else
            {
                yield return rec with { Timestamp = await _ghostService.DownloadGhostTimestampAsync(rec.ReplayUrl) };
                await Task.Delay(500);
            }
        }
    }

    public async Task<RecordChangesTM2> CheckJustWorldRecordAsync(MapModel map,
                                                                  IEnumerable<TM2Record> currentRecords,
                                                                  Dictionary<string, LoginModel> loginModels,
                                                                  bool isFromManialink,
                                                                  CancellationToken cancellationToken)
    {
        var wrChange = await CheckWorldRecordAsync(map, currentRecords, loginModels, isFromManialink, cancellationToken);

        return new(wrChange, NewRecordCount: null, RecordChanges: null, LeaderboardChanges: null);
    }

    // Not usable in parallel
    public async Task<WorldRecordChangeTM2?> CheckWorldRecordAsync(
        MapModel mapModel,
        IEnumerable<TM2Record> currentRecords,
        Dictionary<string, LoginModel> loginModels,
        bool isFromManialink,
        CancellationToken cancellationToken)
    {
        // Do not use the overload, as the cheated records have been already filtered
        var wr = GetWorldRecord(currentRecords, Enumerable.Empty<string>());

        if (wr is null)
        {
            return null;
        }

        var removedWrs = new List<WorldRecordModel>();

        var currentWr = await _wrUnitOfWork.WorldRecords.GetCurrentByMapUidAsync(mapModel.MapUid, cancellationToken);
        var login = loginModels[wr.Login];

        // manialink sync + refresh button issue resolve
        if (currentWr is not null && currentWr.Unverified && wr.Time >= currentWr.TimeInt32)
        {
            return null;
        }

        var previousWr = currentWr;

        // Worse WR is a sign of a removed world record
        while (previousWr is not null && !isFromManialink && !previousWr.Unverified && wr.Time.TotalMilliseconds > previousWr.Time)
        {
            if (previousWr.Ignored != IgnoredMode.NotIgnored)
            {
                previousWr = previousWr.PreviousWorldRecord;
                continue;
            }

            previousWr.Ignored = IgnoredMode.Ignored;

            _logger.LogInformation("Removed WR: {time} by {player}", previousWr.TimeInt32, previousWr.GetPlayerNickname());

            removedWrs.Add(previousWr);

            previousWr = previousWr.PreviousWorldRecord;
        }

        // WR that has been already reported is ignored
        if (previousWr is not null && wr.Time.TotalMilliseconds == previousWr.Time)
        {
            if (removedWrs.Count == 0)
            {
                return null;
            }
            
            return new WorldRecordChangeTM2(null, removedWrs);
        }

        // Non-existing previous record or current wr not equal to previous wr are reported
        var wrModel = await CreateWorldRecordAsync(wr, previousWr, login, mapModel, unverified: isFromManialink, cancellationToken);

        return new WorldRecordChangeTM2(wrModel, removedWrs);
    }

    /// <summary>
    /// Gets the world record with consideration of cheated records.
    /// </summary>
    /// <param name="currentRecords"></param>
    /// <param name="ignoredLoginNames"></param>
    /// <returns></returns>
    private static TM2Record? GetWorldRecord(IEnumerable<TM2Record> currentRecords, IEnumerable<string> ignoredLoginNames)
    {
        return currentRecords.OrderBy(x => x.Rank)
            .FirstOrDefault(x => x.Time > TimeInt32.Zero && !ignoredLoginNames.Contains(x.Login));
    }

    private async Task<WorldRecordModel> CreateWorldRecordAsync(TM2Record wr,
                                                                WorldRecordModel? previousWr,
                                                                LoginModel login,
                                                                MapModel map,
                                                                bool unverified,
                                                                CancellationToken cancellationToken)
    {
        var drivenOn = wr.Timestamp;

        if (drivenOn is null && wr.ReplayUrl is not null)
        {
            try
            {
                using var response = await _http.HeadAsync(wr.ReplayUrl, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var lastModified = response.Content.Headers.LastModified;

                    if (lastModified.HasValue)
                    {
                        drivenOn = lastModified.Value.UtcDateTime;
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "When creating WR: Url {replayUrl} is not usable.", wr.ReplayUrl);
            }
        }

        if (drivenOn is null)
        {
            throw new Exception("No timestamp provided");
        }

        return new WorldRecordModel
        {
            Guid = Guid.NewGuid(),
            Map = map,
            Player = login,
            DrivenOn = drivenOn.Value.UtcDateTime,
            PublishedOn = drivenOn.Value.UtcDateTime,
            ReplayUrl = wr.ReplayUrl,
            Time = wr.Time.TotalMilliseconds,
            PreviousWorldRecord = previousWr,
            Unverified = unverified
        };
    }

    private async IAsyncEnumerable<(TM2Record, DateTimeOffset?)> DownloadMissingGhostsAsync(string mapUid, IEnumerable<TM2Record> records, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var rec in records)
        {
            if (rec.ReplayUrl is null || _ghostService.GhostExists(mapUid, rec.Time, rec.Login))
            {
                continue;
            }

            var timestamp = await _ghostService.DownloadGhostAndGetTimestampAsync(mapUid, rec.ReplayUrl, rec.Time, rec.Login);

            yield return (rec, timestamp);

            await Task.Delay(500, cancellationToken);
        }
    }

    private async Task<Dictionary<string, IGrouping<MapModel, WorldRecordModel>>>
        GetWorldRecordHistoriesByMapGroupAsync(MapGroupModel mapGroup)
    {
        var relevantWrs = await _wrUnitOfWork.WorldRecords.GetHistoriesByMapGroupAsync(mapGroup);

        var wrs = relevantWrs
            .GroupBy(x => x.Map)
            .ToDictionary(x => x.Key.MapUid);

        return wrs;
    }

    private async Task<WorldRecordModel> CreateWorldRecordAsync(LeaderboardRecord record,
        MapModel map, DateTimeOffset recordTimestamp, WorldRecordModel? previousWr,
        DateTime? publishedTimestamp = null)
    {
        var login = await _wrUnitOfWork.Logins.GetOrAddAsync(Game.TM2, record.Login, record.Nickname);

        login.Nickname = record.Nickname;

        return new WorldRecordModel
        {
            Guid = Guid.NewGuid(),
            Map = map,
            Player = login,
            DrivenOn = recordTimestamp.UtcDateTime,
            PublishedOn = publishedTimestamp ?? recordTimestamp.UtcDateTime,
            ReplayUrl = record.ReplayUrl,
            Time = record.Time.TotalMilliseconds,
            PreviousWorldRecord = previousWr,
            Unverified = record.IsFromManialink
        };
    }
}
