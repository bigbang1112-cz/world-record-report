using System.Collections.ObjectModel;
using BigBang1112.WorldRecordReportLib.Enums;
using BigBang1112.WorldRecordReportLib.Models;
using BigBang1112.WorldRecordReportLib.Models.ReportScopes;
using BigBang1112.WorldRecordReportLib.Services.Wrappers;
using ManiaAPI.NadeoAPI;
using Mapster;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TmEssentials;

namespace BigBang1112.WorldRecordReportLib.Services;

public class RefreshTM2020Service : RefreshService
{
    private const string ScopeOfficialWR = $"{nameof(ReportScopeSet.TM2020)}:{nameof(ReportScopeTM2020.Official)}:{nameof(ReportScopeTM2020Official.WR)}";
    private const string ScopeOfficialChanges = $"{nameof(ReportScopeSet.TM2020)}:{nameof(ReportScopeTM2020.Official)}:{nameof(ReportScopeTM2020Official.Changes)}";

    private readonly RefreshScheduleService _refreshSchedule;
    private readonly RecordStorageService _recordStorageService;
    private readonly SnapshotStorageService _snapshotStorageService;
    private readonly ReportService _reportService;
    private readonly IWrUnitOfWork _wrUnitOfWork;
    private readonly INadeoApiService _nadeoApiService;
    private readonly ITrackmaniaApiService _trackmaniaApiService;
    private readonly IGhostService _ghostService;
    private readonly ILogger<RefreshTM2020Service> _logger;

    public RefreshTM2020Service(RefreshScheduleService refreshSchedule,
                                RecordStorageService recordStorageService,
                                SnapshotStorageService snapshotStorageService,
                                ReportService reportService,
                                IWrUnitOfWork wrUnitOfWork,
                                INadeoApiService nadeoApiService,
                                ITrackmaniaApiService trackmaniaApiService,
                                IGhostService ghostService,
                                ILogger<RefreshTM2020Service> logger) : base(logger)
    {
        _refreshSchedule = refreshSchedule;
        _recordStorageService = recordStorageService;
        _snapshotStorageService = snapshotStorageService;
        _reportService = reportService;
        _wrUnitOfWork = wrUnitOfWork;
        _nadeoApiService = nadeoApiService;
        _trackmaniaApiService = trackmaniaApiService;
        _ghostService = ghostService;
        _logger = logger;
    }

    public async Task RefreshOfficialAsync(bool forceUpdate = false, CancellationToken cancellationToken = default)
    {
        var map = _refreshSchedule.NextTM2020OfficialMap();

        if (map is null)
        {
            _logger.LogInformation("Skipping the TM2020 official refresh. No maps found.");
            return;
        }

        await RefreshAsync(map, forceUpdate, cancellationToken);
    }

    public async Task RefreshAsync(MapModel map, bool forceUpdate = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Refreshing TM2020 map: {map}", map.DeformattedName);

        var topLbCollection = await _nadeoApiService.GetTopLeaderboardAsync(map.MapUid, length: 20, cancellationToken: cancellationToken);

        _logger.LogInformation("Leaderboard received.");

        var records = topLbCollection.Top.Top;
        var accountIds = records.Select(x => x.AccountId);

        // Populate with display names that are already known
        var loginModels = await _wrUnitOfWork.Logins.GetByNamesAsync(Game.TM2020, accountIds, cancellationToken);
        
        var hadNullLastNicknameChangeOn = SetLastNicknameChangeOnWhenNull(loginModels);

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
            // Save state around the LoginModel storage
            await _wrUnitOfWork.SaveAsync(cancellationToken);
        }

        var ignoredLoginNames = await _wrUnitOfWork.IgnoredLogins.GetNamesByGameAsync(Game.TM2020, cancellationToken);

        var mapModel = await _wrUnitOfWork.Maps.GetByUidAsync(map.MapUid, cancellationToken) ?? throw new Exception("Map is no longer in the database");

        UpdateLastRefreshedOn(mapModel);

        // Check existance of any leaderboard data in api/v1/records/tm2020/World
        if (_recordStorageService.OfficialLeaderboardExists(Game.TM2020, mapModel.MapUid))
        {
            await CompareLeaderboardAsync(mapModel, records, loginModels, ignoredLoginNames, forceUpdate, cancellationToken);
        }
        else
        {
            // Create a fresh leaderboard (for the first time) where world record is not going to be reported
            await CreateLeaderboardAsync(mapModel, records, loginModels, ignoredLoginNames, accountIds, cancellationToken);
        }

//#if RELEASE
        await _wrUnitOfWork.SaveAsync(cancellationToken);
//#endif
    }

    private static bool SetLastNicknameChangeOnWhenNull(Dictionary<Guid, LoginModel> loginModels)
    {
        // Assign LastNicknameChangeOn that are NULL to UtcNow
        // If any of them is NULL, hadNullLastNicknameChangeOn is true, and UoW will then save the changes
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

        return hadNullLastNicknameChangeOn;
    }

    // This method has a major issue: it creates double WR in the database when the leaderboard files are deleted
    // Solution: use the RefreshTM2Service way
    private async Task CreateLeaderboardAsync(MapModel mapModel,
                                              Record[] records,
                                              Dictionary<Guid, LoginModel> loginModels,
                                              IEnumerable<string> ignoredLoginNames,
                                              IEnumerable<Guid> accountIds,
                                              CancellationToken cancellationToken)
    {
        var currentRecords = await SaveLeaderboardAsync(mapModel, records, accountIds, loginModels, ignoredLoginNames, prevRecords: null, cancellationToken);
        
        // Add the world record to the database without reporting it

        var wr = GetWorldRecord(currentRecords, ignoredLoginNames);

        if (wr is null)
        {
            return;
        }

        var login = loginModels[wr.PlayerId];

        await AddWorldRecordAsync(wr, previousWr: null, login, mapModel, cancellationToken);
    }

    /// <summary>
    /// Gets the world record without considering cheated records.
    /// </summary>
    /// <param name="currentRecords"></param>
    /// <returns></returns>
    private static TM2020Record? GetWorldRecord(List<IRecord<Guid>> currentRecords)
    {
        return currentRecords.FirstOrDefault(x => x.Time > TimeInt32.Zero) as TM2020Record;
    }

    /// <summary>
    /// Gets the world record with consideration of cheated records.
    /// </summary>
    /// <param name="currentRecords"></param>
    /// <param name="ignoredLoginNames"></param>
    /// <returns></returns>
    private static TM2020Record? GetWorldRecord(List<TM2020Record> currentRecords, IEnumerable<string> ignoredLoginNames)
    {
        return currentRecords.FirstOrDefault(x => x.Time > TimeInt32.Zero && !ignoredLoginNames.Contains(x.PlayerId.ToString()));
    }

    private async Task CompareLeaderboardAsync(MapModel map,
                                               Record[] records,
                                               Dictionary<Guid, LoginModel> loginModels,
                                               IEnumerable<string> ignoredLoginNames,
                                               bool forceUpdate,
                                               CancellationToken cancellationToken)
    {
        // Converts the ManiaAPI record objects to the TM2020RecordFundamental struct array
        // This is done to make use of the IRecord interface
        var currentRecordsWithCheatedFundamental = records.Select(x => new TM2020RecordFundamental
        {
            Rank = x.Position,
            AccountId = x.AccountId,
            Score = x.Score
        } as IRecord<Guid>).ToList();

        _logger.LogInformation("Importing the previous leaderboard...");

        // Gets the leaderboard that is already stored (considered previous)
        var previousRecordsWithCheated = await _recordStorageService.GetTM2020LeaderboardAsync(map.MapUid, cancellationToken: cancellationToken);

        if (previousRecordsWithCheated is null)
        {
            // The object shouldn't return a null result, as the JSON is not formatted to behave this way
            throw new Exception("previousRecords is null even though the leaderboard file exists");
        }

        await DownloadMissingGhostsAsync(map, previousRecordsWithCheated, cancellationToken);

        // Leaderboard differences algorithm
        var diffForDownloading = LeaderboardComparer.Compare(currentRecordsWithCheatedFundamental, previousRecordsWithCheated);

        var currentRecordsWithCheated = default(List<TM2020Record>);

        if (diffForDownloading is null)
        {
            _logger.LogInformation("No leaderboard differences found.");

            if (forceUpdate)
            {
                currentRecordsWithCheated = await SaveLeaderboardAsync(map, records, records.Select(x => x.AccountId), loginModels, ignoredLoginNames, previousRecordsWithCheated, cancellationToken);
            }
            else
            {
                return;
            }
        }
        else
        {
            LogDifferences(diffForDownloading);

            // New records and improved records are merged into one collection of accounts for the MapRecords request, to receive ghost URL downloads
            var newAndImprovedRecordAccounts = diffForDownloading.NewRecords
                .Concat(diffForDownloading.ImprovedRecords)
                .Select(x => x.PlayerId);
            
            var timestamp = _recordStorageService.GetOfficialLeaderboardLastUpdatedOn(Game.TM2020, map.MapUid);

            if (timestamp.HasValue)
            {
                await _snapshotStorageService.SaveTM2020LeaderboardAsync(previousRecordsWithCheated, timestamp.Value, map.MapUid, cancellationToken: cancellationToken);
            }
            
            currentRecordsWithCheated = await SaveLeaderboardAsync(map, records, newAndImprovedRecordAccounts, loginModels, ignoredLoginNames, previousRecordsWithCheated, cancellationToken);
        }

        var currentRecordsWithoutCheated = currentRecordsWithCheated
            .Where(x => !ignoredLoginNames.Contains(x.PlayerId.ToString()))
                .Select((x, i) => x with { Rank = i + 1 })
            .Cast<IRecord<Guid>>()
            .ToList();

        var previousRecordsWithoutCheated = previousRecordsWithCheated
            .Where(x => !ignoredLoginNames.Contains(x.PlayerId.ToString()))
                .Select((x, i) => x with { Rank = i + 1 })
            .Cast<IRecord<Guid>>()
            .ToList();

        var diffForReporting = LeaderboardComparer.Compare(currentRecordsWithoutCheated, previousRecordsWithoutCheated);

        // Current records help with managing the reports

        var mapModel = await _wrUnitOfWork.Maps.GetByUidAsync(map.MapUid, cancellationToken) ?? throw new Exception();

        // add record changes
        if (diffForDownloading is not null)
        {
            var goneLoginModels = await _wrUnitOfWork.Logins.GetByNamesAsync(Game.TM2020, diffForDownloading.PushedOffRecords.Concat(diffForDownloading.RemovedRecords).Select(x => x.PlayerId), cancellationToken);

            foreach (var (playerId, loginModel) in goneLoginModels)
            {
                if (!loginModels.ContainsKey(playerId))
                {
                    loginModels[playerId] = loginModel;
                }
            }

            var detailedChanges = CreateListOfRecordChanges(diffForDownloading, mapModel, loginModels);

            foreach (var change in detailedChanges)
            {
                await _wrUnitOfWork.RecordSetDetailedChanges.AddAsync(change, cancellationToken);
            }
        }
        //

        // Do not use the overload, as the cheated records have been already filtered
        var wr = GetWorldRecord(currentRecordsWithoutCheated);

        if (wr is not null)
        {
            var currentWr = await _wrUnitOfWork.WorldRecords.GetCurrentByMapUidAsync(map.MapUid, cancellationToken);
            var login = loginModels[wr.PlayerId];

            var previousWr = currentWr;

            var removedWrs = new List<WorldRecordModel>();

            // Worse WR is a sign of a removed world record
            while (previousWr is not null && wr.Time.TotalMilliseconds > previousWr.Time)
            {
                if (previousWr.Ignored)
                {
                    previousWr = previousWr.PreviousWorldRecord;
                    continue;
                }

                previousWr.Ignored = true;

                _logger.LogInformation("Removed WR: {time} by {player}", previousWr.TimeInt32, previousWr.GetPlayerNickname());

                // Remove the discord webhook message
                await _reportService.RemoveWorldRecordReportAsync(previousWr, cancellationToken);

                removedWrs.Add(previousWr);

                previousWr = previousWr.PreviousWorldRecord;
            }

            // Non-existing previous record or current wr not equal to previous wr are reported
            if (previousWr is null || wr.Time.TotalMilliseconds != previousWr.Time)
            {
                var wrModel = await AddWorldRecordAsync(wr, previousWr, login, mapModel, cancellationToken);

                if (mapModel.Campaign is null || DateTime.UtcNow - mapModel.Campaign.PublishedOn >= TimeSpan.FromDays(7))
                {
                    await ReportWorldRecordAsync(wrModel, removedWrs, cancellationToken);
                }
            }
            else // WR that has been already reported is ignored
            {
                _logger.LogInformation("Ignoring the report - already reported.");
            }
        }
        
        if (diffForReporting is not null)
        {
            await ReportDifferencesAsync(diffForReporting,
                                         currentRecordsWithoutCheated.ToDictionary(x => x.PlayerId),
                                         previousRecordsWithoutCheated.ToDictionary(x => x.PlayerId),
                                         mapModel,
                                         cancellationToken);
        }
    }

    private static void UpdateLastRefreshedOn(MapModel mapModel)
    {
        var lastRefreshedOn = DateTime.UtcNow;

        if (mapModel.LastRefreshedOn is null)
        {
            mapModel.LastRefreshedOn = new ScoreContextValue<DateTimeOffset>(lastRefreshedOn);
        }
        else
        {
            mapModel.LastRefreshedOn = mapModel.LastRefreshedOn with { Default = lastRefreshedOn };
        }
    }

    private void LogDifferences(LeaderboardChanges<Guid> diff)
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

    private async Task ReportDifferencesAsync<TPlayerId>(LeaderboardChanges<TPlayerId> diff,
                                              Dictionary<TPlayerId, IRecord<TPlayerId>> currentRecords,
                                              Dictionary<TPlayerId, IRecord<TPlayerId>> previousRecords,
                                              MapModel map,
                                              CancellationToken cancellationToken) where TPlayerId : notnull
    {
        var changes = CreateLeaderboardChangesRich(diff, currentRecords, previousRecords);

        if (changes is null)
        {
            return;
        }

        await _reportService.ReportDifferencesAsync(changes, map, ScopeOfficialChanges, cancellationToken: cancellationToken);
    }

    private async Task ReportWorldRecordAsync(WorldRecordModel wr, IEnumerable<WorldRecordModel> removedWrs, CancellationToken cancellationToken)
    {
        _logger.LogInformation("New WR: {time} by {player}", wr.TimeInt32, wr.GetPlayerNicknameDeformatted());

        if (removedWrs.Any())
        {
            await _reportService.ReportRemovedWorldRecordsAsync(wr, removedWrs, ScopeOfficialWR, cancellationToken);
        }
        else
        {
            await _reportService.ReportWorldRecordAsync(wr, ScopeOfficialWR, cancellationToken);
        }
    }

    private async Task<WorldRecordModel> AddWorldRecordAsync(TM2020Record wr, WorldRecordModel? previousWr, LoginModel login, MapModel map, CancellationToken cancellationToken)
    {
        var wrModel = new WorldRecordModel
        {
            Guid = Guid.NewGuid(),
            Map = map,
            Player = login,
            DrivenOn = wr.Timestamp,
            PublishedOn = wr.Timestamp,
            ReplayUrl = wr.GhostUrl,
            Time = wr.Time.TotalMilliseconds,
            PreviousWorldRecord = previousWr
        };

        await _wrUnitOfWork.WorldRecords.AddAsync(wrModel, cancellationToken);

        return wrModel;
    }

    private async Task<List<TM2020Record>> SaveLeaderboardAsync(MapModel mapModel,
                                                                Record[] records,
                                                                IEnumerable<Guid> accountIds,
                                                                Dictionary<Guid, LoginModel> loginModels,
                                                                IEnumerable<string> ignoredLoginNames,
                                                                ReadOnlyCollection<TM2020Record>? prevRecords,
                                                                CancellationToken cancellationToken)
    {
        _logger.LogInformation("{map}: Saving the leaderboard...", mapModel.DeformattedName);

        // Map GUID (not UID) is required for the MapRecords request to receive the ghost urls
        var mapId = mapModel.MapId ?? throw new Exception();

        var recordDetails = await _nadeoApiService.GetMapRecordsAsync(accountIds, mapId.Yield(), cancellationToken);

        // MapRecord dictionary is used to quickly find url and timestamp
        var recordDict = recordDetails.ToDictionary(x => x.AccountId);

        // Previous records dictionary is used to quickly assign previous records to its expected ranks
        var prevRecordsDict = prevRecords?.ToDictionary(x => x.PlayerId);

        // Compile the mess into a nice list of current leaderboard records with all its details
        var tm2020Records = RecordsToTM2020Records(records, ignoredLoginNames, loginModels, recordDict, prevRecordsDict).ToList();

//#if RELEASE
        await _recordStorageService.SaveTM2020LeaderboardAsync(tm2020Records, mapModel.MapUid, cancellationToken: cancellationToken);
//#endif

        _logger.LogInformation("{map}: Leaderboard saved.", mapModel.DeformattedName);

        // Download ghosts from recordDetails to the Ghosts folder in this part of the code
        foreach (var rec in recordDetails)
        {
            if (rec.Url is null || _ghostService.GhostExists(mapModel.MapUid, rec.RecordScore.Time, rec.AccountId.ToString()))
            {
                continue;
            }

            _ = await _ghostService.DownloadGhostAndGetTimestampAsync(mapModel.MapUid, rec.Url, rec.RecordScore.Time, rec.AccountId.ToString());
            await Task.Delay(500, cancellationToken);
        }

        return tm2020Records;
    }

    private async Task DownloadMissingGhostsAsync(MapModel map, IEnumerable<TM2020Record> records, CancellationToken cancellationToken)
    {
        foreach (var rec in records)
        {
            var playerIdStr = rec.PlayerId.ToString();

            if (rec.GhostUrl is null || _ghostService.GhostExists(map.MapUid, rec.Time, playerIdStr))
            {
                continue;
            }

            _ = await _ghostService.DownloadGhostAndGetTimestampAsync(map.MapUid, rec.GhostUrl, rec.Time, playerIdStr);
            await Task.Delay(500, cancellationToken);
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
                    Time = rec.Score,
                    PlayerId = rec.AccountId,
                    DisplayName = loginModels[rec.AccountId].Nickname,
                    GhostUrl = recDetails.Url,
                    Timestamp = recDetails.Timestamp.UtcDateTime,
                    Ignored = isIgnored
                };

                continue;
            }
            
            if (prevRecords is not null)
            {
                if (prevRecords[rec.AccountId].Ignored != isIgnored)
                {
                    prevRecords[rec.AccountId] = prevRecords[rec.AccountId] with { Ignored = isIgnored };
                }

                yield return prevRecords[rec.AccountId]; // Uses an existing record from the previous records

                continue;
            }

            // Reaching this place could corrupt the leaderboard, so it is prefered to close the update of this leaderboard early
            throw new Exception("This should not happen");
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
