using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using BigBang1112.WorldRecordReportLib.Enums;
using BigBang1112.WorldRecordReportLib.Services;
using BigBang1112.WorldRecordReportLib.Services.Wrappers;
using ManiaAPI.NadeoAPI;
using ManiaAPI.TrackmaniaAPI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;

namespace BigBang1112.WorldRecordReportLib.Jobs;

public class RefreshTM2020OfficialJob : IJob
{
    private readonly IConfiguration _config;
    private readonly RefreshScheduleService _refreshSchedule;
    private readonly IWrUnitOfWork _wrUnitOfWork;
    private readonly INadeoApiService _nadeoApiService;
    private readonly ITrackmaniaApiService _trackmaniaApiService;
    private readonly ILogger<RefreshTM2020OfficialJob> _logger;

    public RefreshTM2020OfficialJob(IConfiguration config,
                                    RefreshScheduleService refreshSchedule,
                                    IWrUnitOfWork wrUnitOfWork,
                                    INadeoApiService nadeoApiService,
                                    ITrackmaniaApiService trackmaniaApiService,
                                    ILogger<RefreshTM2020OfficialJob> logger)
    {
        _config = config;
        _refreshSchedule = refreshSchedule;
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
            return;
        }

        var topLbCollection = await _nadeoApiService.GetTopLeaderboardAsync(map.MapUid, cancellationToken: cancellationToken);
        
        var records = topLbCollection.Top.Top;
        var accountIds = records.Select(x => x.AccountId);

        var hadNullLastNicknameChangeOn = false;

        var loginModels = await _wrUnitOfWork.Logins.GetByNamesAsync(Game.TM2020, accountIds, cancellationToken);

        foreach (var (accountId, loginModel) in loginModels)
        {
            if (loginModel.LastNicknameChangeOn is null)
            {
                loginModel.LastNicknameChangeOn = DateTime.UtcNow;
                hadNullLastNicknameChangeOn = true;
            }
        }

        var accountIdsToRequest = accountIds.Where(x => !loginModels.ContainsKey(x));
        var anyAccountIdsToRequest = accountIdsToRequest.Any();

        if (anyAccountIdsToRequest)
        {
            await RequestDisplayNamesAsync(accountIdsToRequest, cancellationToken);
        }

        if (anyAccountIdsToRequest || hadNullLastNicknameChangeOn)
        {
            await _wrUnitOfWork.SaveAsync(cancellationToken);
        }

        // LeaderboardComparer.Compare()

        var mapId = await _wrUnitOfWork.Maps.GetMapIdByMapUidAsync(map.MapUid, cancellationToken) ?? throw new Exception();

        var recs = await _nadeoApiService.GetMapRecordsAsync(accountIds, Enumerable.Repeat(mapId, 1), cancellationToken);
    }

    private async Task RequestDisplayNamesAsync(IEnumerable<Guid> accountIdsToRequest, CancellationToken cancellationToken)
    {
        var displayNames = await _trackmaniaApiService.GetDisplayNamesAsync(accountIdsToRequest, cancellationToken);

        var game = await _wrUnitOfWork.Games.GetAsync(Game.TM2020, cancellationToken);

        await _wrUnitOfWork.Logins.AddRangeAsync(displayNames.Select(x => new LoginModel
        {
            Game = game,
            Name = x.Key.ToString(),
            Nickname = x.Value,
            LastNicknameChangeOn = DateTime.UtcNow
        }), cancellationToken);
    }
}
