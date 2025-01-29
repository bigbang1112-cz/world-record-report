using System.Collections.Immutable;
using ManiaAPI.TrackmaniaAPI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BigBang1112.WorldRecordReportLib.Services.Wrappers;

public class TrackmaniaApiService : IHostedService, ITrackmaniaApiService
{
    private readonly IConfiguration _config;
    private readonly TrackmaniaAPI _trackmaniaApi;
    private readonly ILogger<TrackmaniaApiService> _logger;

    public TrackmaniaApiService(IConfiguration config, TrackmaniaAPI trackmaniaApi, ILogger<TrackmaniaApiService> logger)
    {
        _config = config;
        _trackmaniaApi = trackmaniaApi;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var clientId = _config["OAuth2:Trackmania:Id"];
        var clientSecret = _config["OAuth2:Trackmania:Secret"];

        _logger.LogInformation("Authorizing with Trackmania API...");

        await _trackmaniaApi.AuthorizeAsync(clientId, clientSecret, cancellationToken);

        _logger.LogInformation("Authorized with Trackmania API.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task<ImmutableDictionary<Guid, string>> GetDisplayNamesAsync(IEnumerable<Guid> accountIds, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("HTTP request: DisplayNames (accountIds={accountIds})", string.Join(',', accountIds));

        return await _trackmaniaApi.GetDisplayNamesAsync(accountIds, cancellationToken);
    }

    public async Task<User> GetUserAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("HTTP request: DisplayNames");

        return await _trackmaniaApi.GetUserAsync(cancellationToken);
    }
}
