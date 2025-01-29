using System.Collections.Immutable;
using ManiaAPI.NadeoAPI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BigBang1112.WorldRecordReportLib.Services.Wrappers;

public class NadeoApiService : IHostedService, INadeoApiService
{
    private readonly IConfiguration _config;
    private readonly NadeoServices _nadeoServices;
    private readonly NadeoLiveServices _nadeoLiveServices;
    private readonly ILogger<NadeoApiService> _logger;

    public NadeoApiService(IConfiguration config,
                           NadeoServices nadeoServices,
                           NadeoLiveServices nadeoLiveServices,
                           ILogger<NadeoApiService> logger)
    {
        _config = config;
        _nadeoServices = nadeoServices;
        _nadeoLiveServices = nadeoLiveServices;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var login = _config["TM2020:DedicatedServer:Login"];
        var password = _config["TM2020:DedicatedServer:Password"];

        _logger.LogInformation("Authorizing with NadeoServices...");

        await _nadeoServices.AuthorizeAsync(login, password, AuthorizationMethod.DedicatedServer, cancellationToken);

        _logger.LogInformation("Authorized with NadeoServices. Authorizing with NadeoLiveServices...");

        await _nadeoLiveServices.AuthorizeAsync(login, password, AuthorizationMethod.DedicatedServer, cancellationToken);

        _logger.LogInformation("Authorized with NadeoLiveServices.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /*public async Task<string> GetAccountDisplayNamesAsync(IEnumerable<Guid> accountIds, CancellationToken cancellationToken = default)
    {
        return await _nadeoServices.GetAccountDisplayNamesAsync(accountIds, cancellationToken);
    }*/

    public async Task<ImmutableArray<MapRecord>> GetMapRecordsAsync(IEnumerable<Guid> accountIds, IEnumerable<Guid> mapIds, CancellationToken cancellationToken = default)
    {
        if (!accountIds.Any() || !mapIds.Any())
        {
            return ImmutableArray<MapRecord>.Empty;
        }

        _logger.LogInformation("HTTP request: MapRecords (accountIds={accountIds}; mapIds={mapIds})", string.Join(',', accountIds), string.Join(',', mapIds));

        return await _nadeoServices.GetMapRecordsAsync(accountIds, mapIds, cancellationToken);
    }

    public async Task<ImmutableArray<MapRecord>> GetMapRecordsAsync(IEnumerable<Guid> accountIds, Guid mapId, CancellationToken cancellationToken = default)
    {
        if (!accountIds.Any())
        {
            return ImmutableArray<MapRecord>.Empty;
        }

        _logger.LogInformation("HTTP request: MapRecords (accountIds={accountIds}; mapId={mapId})", string.Join(',', accountIds), mapId);

        return await _nadeoServices.GetMapRecordsAsync(accountIds, mapId, cancellationToken);
    }

    public async ValueTask<bool> RefreshAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("HTTP request: Refresh");
        
        return await _nadeoServices.RefreshAsync(cancellationToken)
            && await _nadeoLiveServices.RefreshAsync(cancellationToken);
    }

    public async Task<TopLeaderboardCollection> GetTopLeaderboardAsync(string mapUid, int length = 10, int offset = 0, bool onlyWorld = true, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("HTTP request: TopLeaderboard (mapUid={mapUid}; length={length}; offset={offset}; onlyWorld={onlyWorld})", mapUid, length, offset, onlyWorld);
        
        return await _nadeoLiveServices.GetTopLeaderboardAsync(mapUid, length, offset, onlyWorld, cancellationToken);
    }

    public async Task<TopLeaderboardCollection> GetTopLeaderboardAsync(string mapUid, string groupId, int length = 10, int offset = 0, bool onlyWorld = true, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("HTTP request: TopLeaderboard (mapUid={mapUid}; groupId={groupId}, length={length}; offset={offset}; onlyWorld={onlyWorld})", mapUid, groupId, length, offset, onlyWorld);

        return await _nadeoLiveServices.GetTopLeaderboardAsync(mapUid, groupId, length, offset, onlyWorld, cancellationToken);
    }
}
