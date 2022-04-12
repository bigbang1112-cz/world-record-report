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

        _logger.LogInformation("Refreshing TM2020 official map... {map}", map);

        var topLbCollection = await _nadeoApiService.GetTopLeaderboardAsync(map.MapUid, length: 20, cancellationToken: cancellationToken);

        _logger.LogInformation("Leaderboard received. {map}", map);
        
        var records = topLbCollection.Top.Top;
        var accountIds = records.Select(x => x.AccountId);

        var hadNullLastNicknameChangeOn = false;

        var loginModels = await _wrUnitOfWork.Logins.GetByNamesAsync(Game.TM2020, accountIds, cancellationToken);
        
        foreach (var (accountId, loginModel) in loginModels)
        {
            if (loginModel.LastNicknameChangeOn is not null)
            {
                continue;
            }
            
            loginModel.LastNicknameChangeOn = DateTime.UtcNow;
            hadNullLastNicknameChangeOn = true;
        }

        var accountIdsToRequest = accountIds.Where(x =>
        {
            if (!loginModels.TryGetValue(x, out LoginModel? login))
            {
                return true;
            }

            return login.LastNicknameChangeOn.HasValue && DateTime.UtcNow - login.LastNicknameChangeOn.Value >= TimeSpan.FromHours(12);
        }).ToList();

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
            // Compare the current leaderboard with the prev one with

            var currentRecordsFundamental = records.Adapt<TM2020RecordFundamental[]>();
            var previousRecords = await _recordStorageService.GetTM2020LeaderboardAsync(map.MapUid, cancellationToken: cancellationToken);

            var diff = LeaderboardComparer.Compare(currentRecordsFundamental.Cast<IRecord<Guid>>(), previousRecords);
            
            if (diff is null)
            {
                return;
            }

            var newAndImprovedRecords = diff.NewRecords.Concat(diff.ImprovedRecords);

            var mapId = await _wrUnitOfWork.Maps.GetMapIdByMapUidAsync(map.MapUid, cancellationToken) ?? throw new Exception();
            var recordDetails = await _nadeoApiService.GetMapRecordsAsync(newAndImprovedRecords.Select(x => x.PlayerId), mapId.Yield(), cancellationToken);
            
            // Get only new and improved records, and request GetMapRecordsAsync only on those
            // Then use the RecordsToTM2020Records
        }
        else
        {
            // If it doesn't exist, call GetMapRecordsAsync on all accountIds
            var mapId = await _wrUnitOfWork.Maps.GetMapIdByMapUidAsync(map.MapUid, cancellationToken) ?? throw new Exception();
            var recordDetails = await _nadeoApiService.GetMapRecordsAsync(accountIds, mapId.Yield(), cancellationToken);
            var recordDict = recordDetails.ToDictionary(x => x.AccountId);

            // Pass the result to RecordsToTM2020Records
            var tm2020Records = RecordsToTM2020Records(records, loginModels, recordDict).ToList();

            await _recordStorageService.SaveTM2020LeaderboardAsync(tm2020Records, map.MapUid, cancellationToken: cancellationToken);
        }
    }

    private static IEnumerable<TM2020Record> RecordsToTM2020Records(Record[] records,
                                                                    Dictionary<Guid, LoginModel> loginModels,
                                                                    Dictionary<Guid, MapRecord> recordDetails)
    {
        foreach (var rec in records)
        {
            yield return new TM2020Record
            {
                Time = rec.Score.TotalMilliseconds,
                PlayerId = rec.AccountId,
                DisplayName = loginModels[rec.AccountId].Nickname,
                GhostUrl = recordDetails[rec.AccountId].Url,
                Timestamp = recordDetails[rec.AccountId].Timestamp.UtcDateTime,
            };
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
            if (loginModels.TryGetValue(accountId, out LoginModel? login))
            {
                login.Nickname = displayName;
                login.LastNicknameChangeOn = DateTime.UtcNow;

                continue;
            }

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
