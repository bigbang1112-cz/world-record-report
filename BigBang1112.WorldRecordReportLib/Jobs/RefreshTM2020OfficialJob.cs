using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using BigBang1112.WorldRecordReportLib.Enums;
using BigBang1112.WorldRecordReportLib.Models;
using BigBang1112.WorldRecordReportLib.Services;
using BigBang1112.WorldRecordReportLib.Services.Wrappers;
using ManiaAPI.NadeoAPI;
using ManiaAPI.TrackmaniaAPI;
using Mapster;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;
using TmEssentials;

namespace BigBang1112.WorldRecordReportLib.Jobs;

public class RefreshTM2020OfficialJob : IJob
{
    private readonly IConfiguration _config;
    private readonly RefreshScheduleService _refreshSchedule;
    private readonly RecordStorageService _recordStorageService;
    private readonly IWrUnitOfWork _wrUnitOfWork;
    private readonly INadeoApiService _nadeoApiService;
    private readonly ITrackmaniaApiService _trackmaniaApiService;
    private readonly IGhostService _ghostService;
    private readonly ILogger<RefreshTM2020OfficialJob> _logger;

    public RefreshTM2020OfficialJob(IConfiguration config,
                                    RefreshScheduleService refreshSchedule,
                                    RecordStorageService recordStorageService,
                                    IWrUnitOfWork wrUnitOfWork,
                                    INadeoApiService nadeoApiService,
                                    ITrackmaniaApiService trackmaniaApiService,
                                    IGhostService ghostService,
                                    ILogger<RefreshTM2020OfficialJob> logger)
    {
        _config = config;
        _refreshSchedule = refreshSchedule;
        _recordStorageService = recordStorageService;
        _wrUnitOfWork = wrUnitOfWork;
        _nadeoApiService = nadeoApiService;
        _trackmaniaApiService = trackmaniaApiService;
        _ghostService = ghostService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await RefreshAsync(forceUpdate: true);
    }

    public async Task RefreshAsync(bool forceUpdate, CancellationToken cancellationToken = default)
    {
        var map = _refreshSchedule.NextTM2020OfficialMap();

        if (map is null)
        {
            _logger.LogInformation("Skipping the TM2020 official refresh. No maps found.");
            return;
        }

        _logger.LogInformation("Refreshing TM2020 official map: {map}", map);

        var topLbCollection = await _nadeoApiService.GetTopLeaderboardAsync(map.MapUid, length: 20, cancellationToken: cancellationToken);

        _logger.LogInformation("Leaderboard received.");
        
        var records = topLbCollection.Top.Top;
        var accountIds = records.Select(x => x.AccountId);

        // Populate with display names that are already known
        var loginModels = await _wrUnitOfWork.Logins.GetByNamesAsync(Game.TM2020, accountIds, cancellationToken);

        // Assign LastNicknameChangeOn that are NULL to UtcNow
        // If any of them is NULL, hadNullLastNicknameChangeOn is true, and UoW will save the changes
        var hadNullLastNicknameChangeOn = false;
        
        foreach (var (accountId, loginModel) in loginModels)
        {
            if (loginModel.LastNicknameChangeOn is not null)
            {
                continue;
            }

            loginModel.LastNicknameChangeOn = DateTime.UtcNow;
            hadNullLastNicknameChangeOn = true;
        }
        //

        // Filter out requests of display names only to the ones that fulfill two criteria
        var accountIdsToRequest = accountIds.Where(x =>
        {
            // Either if it doesn't exist in the database
            if (!loginModels.TryGetValue(x, out LoginModel? login))
            {
                return true;
            }

            // Or if its been 1 hour since last nickname refresh
            // Better name could be something like LastNicknameCheckOn
            return login.LastNicknameChangeOn.HasValue && DateTime.UtcNow - login.LastNicknameChangeOn.Value >= TimeSpan.FromHours(12);
        }).ToList(); // ToList ensures these conditions are evaluated just once, but as only Any() would be called without it, it could change in the future

        var anyAccountIdsToRequest = accountIdsToRequest.Count > 0;

        if (anyAccountIdsToRequest)
        {
            await RequestAndAddDisplayNamesAsync(loginModels, accountIdsToRequest, cancellationToken);
        }

        if (anyAccountIdsToRequest || hadNullLastNicknameChangeOn)
        {
            await _wrUnitOfWork.SaveAsync(cancellationToken);
        }
        
        var ignoredLoginNames = await _wrUnitOfWork.IgnoredLogins.GetNamesByGameAsync(Game.TM2020, cancellationToken);

        // Check existance of any leaderboard data in api/v1/records/tm2020/World, possibly through RecordStorageService
        if (_recordStorageService.OfficialLeaderboardExists(Game.TM2020, map.MapUid))
        {
            await CompareLeaderboardAsync(map, records, loginModels, ignoredLoginNames, forceUpdate, cancellationToken);
        }
        else
        {
            var currentRecords = await SaveLeaderboardAsync(map, records, accountIds, loginModels, ignoredLoginNames, prevRecords: null, cancellationToken);
            var wr = GetWorldRecord(currentRecords, ignoredLoginNames);

            if (wr is not null)
            {
                var login = loginModels[wr.PlayerId];
                var mapModel = await _wrUnitOfWork.Maps.GetByUidAsync(map.MapUid, cancellationToken) ?? throw new Exception();

                await AddWorldRecordAsync(wr, previousWr: null, login, mapModel, cancellationToken);
            }
        }
        
        await _wrUnitOfWork.SaveAsync(cancellationToken);
    }
        
    private static TM2020Record? GetWorldRecord(List<TM2020Record> currentRecords, IEnumerable<string> ignoredLoginNames)
    {        
        return currentRecords.FirstOrDefault(x => !ignoredLoginNames.Contains(x.PlayerId.ToString()));
    }

    private async Task CompareLeaderboardAsync(MapRefreshData map,
                                               Record[] records,
                                               Dictionary<Guid, LoginModel> loginModels,
                                               IEnumerable<string> ignoredLoginNames,
                                               bool forceUpdate,
                                               CancellationToken cancellationToken)
    {
        // Converts the ManiaAPI record objects to the TM2020RecordFundamental struct array
        // This is done to make use of the IRecord interface
        var currentRecordsFundamental = records.Adapt<TM2020RecordFundamental[]>();

        _logger.LogInformation("Importing the previous leaderboard...");

        // Gets the leaderboard that is already stored (considered previous)
        var previousRecords = await _recordStorageService.GetTM2020LeaderboardAsync(map.MapUid, cancellationToken: cancellationToken);

        if (previousRecords is null)
        {
            // The object shouldn't return a null result, as the JSON is not formatted to behave this way
            throw new Exception("previousRecords is null even though the leaderboard file exists");
        }

        await DownloadMissingGhostsAsync(map, previousRecords, cancellationToken);

        // Leaderboard differences algorithm
        // For structs arrays, Cast is unfortunately required for some reason
        var diff = LeaderboardComparer.Compare(currentRecordsFundamental.Cast<IRecord<Guid>>(), previousRecords);

        var currentRecords = default(List<TM2020Record>);

        if (diff is null)
        {
            _logger.LogInformation("No leaderboard differences found.");

            if (forceUpdate)
            {
                currentRecords = await SaveLeaderboardAsync(map, records, records.Select(x => x.AccountId), loginModels, ignoredLoginNames, previousRecords, cancellationToken);
            }
            else
            {
                return;
            }
        }
        else
        {
            LogDifferences(diff);

            // New records and improved records are merged into one collection of accounts for the MapRecords request, to receive ghost URL downloads
            var newAndImprovedRecordAccounts = diff.NewRecords
                .Concat(diff.ImprovedRecords)
                .Select(x => x.PlayerId);

            currentRecords = await SaveLeaderboardAsync(map, records, newAndImprovedRecordAccounts, loginModels, ignoredLoginNames, previousRecords, cancellationToken);
        }
        
        // Current records help with managing the reports

        var wr = GetWorldRecord(currentRecords, ignoredLoginNames);

        if (wr is not null)
        {
            var currentWr = await _wrUnitOfWork.WorldRecords.GetCurrentByMapUidAsync(map.MapUid, cancellationToken);
            var login = loginModels[wr.PlayerId];
            var mapModel = await _wrUnitOfWork.Maps.GetByUidAsync(map.MapUid, cancellationToken) ?? throw new Exception();

            if (currentWr is null)
            {
                await AddWorldRecordAsync(wr, previousWr: null, login, mapModel, cancellationToken);
            }
            else
            {
                await ReportWorldRecordAsync(wr, previousWr: currentWr, login, mapModel, cancellationToken);
            }
        }

        if (diff is not null)
        {
            await ReportDifferencesAsync(diff,
                                         currentRecords.ToDictionary(x => x.PlayerId),
                                         previousRecords.ToDictionary(x => x.PlayerId),
                                         cancellationToken);
        }
    }

    private void LogDifferences(Top10Changes<Guid> diff)
    {
        _logger.LogInformation("Differences found between the current and previous leaderboard.");

        if (diff.NewRecords.Any())
        {
            _logger.LogInformation("New records: {count}", diff.NewRecords.Count());
        }

        if (diff.ImprovedRecords.Any())
        {
            _logger.LogInformation("Improved records: {count}", diff.ImprovedRecords.Count());
        }

        if (diff.PushedOffRecords.Any())
        {
            _logger.LogInformation("Pushed off records: {count}", diff.PushedOffRecords.Count());
        }

        if (diff.RemovedRecords.Any())
        {
            _logger.LogInformation("Removed records: {count}", diff.RemovedRecords.Count());
        }

        if (diff.WorsenRecords.Any())
        {
            _logger.LogInformation("Worsened records: {count}", diff.WorsenRecords.Count());
        }
    }

    private async Task ReportDifferencesAsync(Top10Changes<Guid> diff,
                                              Dictionary<Guid, TM2020Record> currentRecords,
                                              Dictionary<Guid, TM2020Record> previousRecords,
                                              CancellationToken cancellationToken)
    {
        if (currentRecords.Count == 0)
        {
            // Can happen if record/s got removed to a point there are none in the leaderboard
            return;
        }

        foreach (var rec in diff.NewRecords)
        {
            var currentRecord = currentRecords[rec.PlayerId];

            _logger.LogInformation("New record: {rank}) {time} by {player}", currentRecord.Rank, new TimeInt32(currentRecord.Time), currentRecord.DisplayName);
        }

        foreach (var rec in diff.ImprovedRecords)
        {
            var previousTime = new TimeInt32(rec.Time);
            var currentRecord = currentRecords[rec.PlayerId];
            var prevRecord = previousRecords[rec.PlayerId];

            var currentTime = new TimeInt32(currentRecord.Time);

            _logger.LogInformation("Improved record: {previousRank}) {previousTime} to {currentRank}) {currentTime} by {player}", prevRecord.Rank, previousTime, currentRecord.Rank, currentTime, currentRecord.DisplayName);
        }

        foreach (var rec in diff.RemovedRecords)
        {
            var prevRecord = previousRecords[rec.PlayerId];

            _logger.LogInformation("Removed record: {rank}) {time} by {player}", prevRecord.Rank, new TimeInt32(prevRecord.Time), prevRecord.DisplayName);
        }

        foreach (var rec in diff.PushedOffRecords)
        {
            var prevRecord = previousRecords[rec.PlayerId];

            _logger.LogInformation("Pushed off record: {rank}) {time} by {player}", prevRecord.Rank, new TimeInt32(prevRecord.Time), prevRecord.DisplayName);
        }

        foreach (var rec in diff.WorsenRecords)
        {
            var currentRecord = currentRecords[rec.PlayerId];

            _logger.LogInformation("Worsened record: {time} by {player}", new TimeInt32(currentRecord.Time), currentRecord.DisplayName);
        }
    }

    private async Task ReportWorldRecordAsync(TM2020Record wr, WorldRecordModel? previousWr, LoginModel login, MapModel map, CancellationToken cancellationToken)
    {
        if (wr.Time == 0)
        {
            _logger.LogInformation("World record is 0 seconds, ignoring.");
            return;
        }

        if (previousWr is not null)
        {
            // Worse WR is a sign of a removed world record
            while (previousWr is not null && wr.Time > previousWr.Time)
            {
                if (previousWr.Ignored)
                {
                    previousWr = previousWr.PreviousWorldRecord;
                    continue;
                }

                previousWr.Ignored = true;

                _logger.LogInformation("Removed WR: {time} by {player}", new TimeInt32(previousWr.Time), previousWr.GetPlayerNickname());

                previousWr = previousWr.PreviousWorldRecord;
            }
            
            // Equal WR is ignored
            if (previousWr is not null && wr.Time == previousWr.Time)
            {
                return;
            }
        }

        _logger.LogInformation("New WR: {time} by {player}", new TimeInt32(wr.Time), wr.DisplayName);

        await AddWorldRecordAsync(wr, previousWr, login, map, cancellationToken);
    }

    private async Task AddWorldRecordAsync(TM2020Record wr, WorldRecordModel? previousWr, LoginModel login, MapModel map, CancellationToken cancellationToken)
    {
        var wrModel = new WorldRecordModel
        {
            Guid = Guid.NewGuid(),
            Map = map,
            Player = login,
            DrivenOn = wr.Timestamp,
            PublishedOn = wr.Timestamp,
            ReplayUrl = wr.GhostUrl,
            Time = wr.Time,
            PreviousWorldRecord = previousWr
        };

        await _wrUnitOfWork.WorldRecords.AddAsync(wrModel, cancellationToken);
    }

    private async Task<List<TM2020Record>> SaveLeaderboardAsync(MapRefreshData map,
                                                                Record[] records,
                                                                IEnumerable<Guid> accountIds,
                                                                Dictionary<Guid, LoginModel> loginModels,
                                                                IEnumerable<string> ignoredLoginNames,
                                                                ReadOnlyCollection<TM2020Record>? prevRecords,
                                                                CancellationToken cancellationToken)
    {
        _logger.LogInformation("{map}: Saving the leaderboard...", map);

        // Map GUID (not UID) is required for the MapRecords request to receive the ghost urls
        var mapId = await _wrUnitOfWork.Maps.GetMapIdByMapUidAsync(map.MapUid, cancellationToken) ?? throw new Exception();

        var recordDetails = await _nadeoApiService.GetMapRecordsAsync(accountIds, mapId.Yield(), cancellationToken);

        // MapRecord dictionary is used to quickly find url and timestamp
        var recordDict = recordDetails.ToDictionary(x => x.AccountId);

        // Previous records dictionary is used to quickly assign previous records to its expected ranks
        var prevRecordsDict = prevRecords?.ToDictionary(x => x.PlayerId);

        // Compile the mess into a nice list of current leaderboard records with all its details
        var tm2020Records = RecordsToTM2020Records(records, ignoredLoginNames, loginModels, recordDict, prevRecordsDict).ToList();

//#if RELEASE
        await _recordStorageService.SaveTM2020LeaderboardAsync(tm2020Records, map.MapUid, cancellationToken: cancellationToken);
//#endif
        
        _logger.LogInformation("{map}: Leaderboard saved.", map);

        // Download ghosts from recordDetails to the Ghosts folder in this part of the code
        foreach (var rec in recordDetails)
        {
            _ = await _ghostService.DownloadGhostAndGetTimestampAsync(map.MapUid, rec.Url, rec.RecordScore.Time, rec.AccountId.ToString());
            await Task.Delay(500, cancellationToken);
        }

        return tm2020Records;
    }

    private async Task DownloadMissingGhostsAsync(MapRefreshData map, IEnumerable<TM2020Record> records, CancellationToken cancellationToken)
    {
        foreach (var rec in records)
        {
            var playerIdStr = rec.PlayerId.ToString();

            if (rec.GhostUrl is not null && !_ghostService.GhostExists(map.MapUid, new TimeInt32(rec.Time), playerIdStr))
            {
                _ = await _ghostService.DownloadGhostAndGetTimestampAsync(map.MapUid, rec.GhostUrl, new TimeInt32(rec.Time), playerIdStr);
                await Task.Delay(500, cancellationToken);
            }
        }
    }

    private static IEnumerable<TM2020Record> RecordsToTM2020Records(Record[] records,
                                                                    IEnumerable<string> ignoredLoginNames,
                                                                    Dictionary<Guid, LoginModel> loginModels,
                                                                    Dictionary<Guid, MapRecord> recordDetails,
                                                                    Dictionary<Guid, TM2020Record>? prevRecords)
    {
        foreach (var rec in records)
        {
            var isIgnored = ignoredLoginNames.Contains(rec.AccountId.ToString());

            if (recordDetails.TryGetValue(rec.AccountId, out MapRecord? recDetails))
            {
                // Create a new record that just got fetched as a fresh record (aka it didn't exist before)

                yield return new TM2020Record
                {
                    Rank = rec.Position,
                    Time = rec.Score.TotalMilliseconds,
                    PlayerId = rec.AccountId,
                    DisplayName = loginModels[rec.AccountId].Nickname,
                    GhostUrl = recDetails.Url,
                    Timestamp = recDetails.Timestamp.UtcDateTime,
                    Ignored = isIgnored
                };
            }
            else if (prevRecords is not null)
            {
                if (prevRecords[rec.AccountId].Ignored != isIgnored)
                {
                    prevRecords[rec.AccountId] = prevRecords[rec.AccountId] with { Ignored = isIgnored };
                }

                yield return prevRecords[rec.AccountId]; // Uses an existing record from the previous records
            }
            else
            {
                // Reaching this place could corrupt the leaderboard, so it is prefered to close the update of this leaderboard early
                throw new Exception("This should not happen");
            }
        }
    }

    /// <summary>
    /// Request the display names of the given account ids and add them to the login models and database (without saving).
    /// </summary>
    /// <param name="loginModels">Dictionary of logins that are currently known, and will be populated further through this method.</param>
    /// <param name="accountIdsToRequest">Account IDs that should receive (or have updated) display names.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task RequestAndAddDisplayNamesAsync(Dictionary<Guid, LoginModel> loginModels, List<Guid> accountIdsToRequest, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Requesting {count} new display names...", accountIdsToRequest.Count);

        var displayNames = await _trackmaniaApiService.GetDisplayNamesAsync(accountIdsToRequest, cancellationToken);

        var game = await _wrUnitOfWork.Games.GetAsync(Game.TM2020, cancellationToken);

        foreach (var (accountId, displayName) in displayNames)
        {
            // The display name is applied on either the login that was already in the database
            if (loginModels.TryGetValue(accountId, out LoginModel? login))
            {
                login.Nickname = displayName;
                login.LastNicknameChangeOn = DateTime.UtcNow;

                continue;
            }

            // Or is created as a fresh login
            login = new LoginModel
            {
                Game = game,
                Name = accountId.ToString(),
                Nickname = displayName,
                LastNicknameChangeOn = DateTime.UtcNow
            };

            loginModels[accountId] = login;

            await _wrUnitOfWork.Logins.AddAsync(login, cancellationToken);
        }

        _logger.LogInformation("Display names received: {names}", string.Join(", ", displayNames.Values));
    }
}
