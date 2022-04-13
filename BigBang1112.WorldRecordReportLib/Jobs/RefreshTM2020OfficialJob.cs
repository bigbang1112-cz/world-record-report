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
    private readonly ILogger<RefreshTM2020OfficialJob> _logger;

    public RefreshTM2020OfficialJob(IConfiguration config,
                                    RefreshScheduleService refreshSchedule,
                                    RecordStorageService recordStorageService,
                                    IWrUnitOfWork wrUnitOfWork,
                                    INadeoApiService nadeoApiService,
                                    ITrackmaniaApiService trackmaniaApiService,
                                    ILogger<RefreshTM2020OfficialJob> logger)
    {
        _config = config;
        _refreshSchedule = refreshSchedule;
        _recordStorageService = recordStorageService;
        _wrUnitOfWork = wrUnitOfWork;
        _nadeoApiService = nadeoApiService;
        _trackmaniaApiService = trackmaniaApiService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await RefreshAsync();
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
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

        // Check existance of any leaderboard data in api/v1/records/tm2020/World, possibly through RecordStorageService
        if (_recordStorageService.OfficialLeaderboardExists(Game.TM2020, map.MapUid))
        {
            await CompareLeaderboardAsync(map, records, loginModels, cancellationToken);
        }
        else
        {
            _ = await SaveLeaderboardAsync(map, records, accountIds, loginModels, prevRecords: null, cancellationToken);
        }
    }

    private async Task CompareLeaderboardAsync(MapRefreshData map, Record[] records, Dictionary<Guid, LoginModel> loginModels, CancellationToken cancellationToken)
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

        // Leaderboard differences algorithm
        // For structs arrays, Cast is unfortunately required for some reason
        var diff = LeaderboardComparer.Compare(currentRecordsFundamental.Cast<IRecord<Guid>>(), previousRecords);

        if (diff is null)
        {
            _logger.LogInformation("No leaderboard differences found.");
            return;
        }

        LogDifferences(diff);

        // New records and improved records are merged into one collection of accounts for the MapRecords request, to receive ghost URL downloads
        var newAndImprovedRecordAccounts = diff.NewRecords
            .Concat(diff.ImprovedRecords)
            .Select(x => x.PlayerId);

        var currentRecords = await SaveLeaderboardAsync(map, records, newAndImprovedRecordAccounts, loginModels, previousRecords, cancellationToken);

        // Current records help with managing the reports
        
        await ReportDifferencesAsync(diff, currentRecords, previousWr: previousRecords.FirstOrDefault(), cancellationToken);
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

    private Task ReportDifferencesAsync(Top10Changes<Guid> diff, List<TM2020Record> currentRecords, TM2020Record? previousWr, CancellationToken cancellationToken)
    {
        if (currentRecords.Count == 0)
        {
            // Can happen if record/s got removed to a point there are none in the leaderboard
            return Task.CompletedTask;
        }

        var possibleWr = currentRecords[0];

        Debug.Assert(possibleWr.Rank == 1);

        foreach (var rec in diff.NewRecords)
        {
            if (rec == possibleWr)
            {
                _logger.LogInformation("New WR: {time} by {player}", new TimeInt32(possibleWr.Time), possibleWr.DisplayName);
                break;
            }
        }

        foreach (var rec in diff.ImprovedRecords)
        {
            if (rec.PlayerId == possibleWr.PlayerId)
            {
                _logger.LogInformation("New WR: {time} by {player}", new TimeInt32(possibleWr.Time), possibleWr.DisplayName);
                break;
            }
        }

        return Task.CompletedTask;
    }

    private async Task<List<TM2020Record>> SaveLeaderboardAsync(MapRefreshData map,
                                                                Record[] records,
                                                                IEnumerable<Guid> accountIds,
                                                                Dictionary<Guid, LoginModel> loginModels,
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
        var tm2020Records = RecordsToTM2020Records(records, loginModels, recordDict, prevRecordsDict).ToList();

        await _recordStorageService.SaveTM2020LeaderboardAsync(tm2020Records, map.MapUid, cancellationToken: cancellationToken);
        
        _logger.LogInformation("{map}: Leaderboard saved.", map);

        // Download ghosts from recordDetails to the Ghosts folder in this part of the code

        return tm2020Records;
    }

    private static IEnumerable<TM2020Record> RecordsToTM2020Records(Record[] records,
                                                                    Dictionary<Guid, LoginModel> loginModels,
                                                                    Dictionary<Guid, MapRecord> recordDetails,
                                                                    Dictionary<Guid, TM2020Record>? prevRecords)
    {
        foreach (var rec in records)
        {
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
                };
            }
            else if (prevRecords is not null)
            {
                // Uses an existing record from the previous records

                yield return prevRecords[rec.AccountId];
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
