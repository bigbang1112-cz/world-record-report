using BigBang1112.WorldRecordReportLib.Enums;
using Microsoft.Extensions.Logging;
using Quartz;
using System.Security.Cryptography;
using TmEssentials;
using ManiaAPI.TrackmaniaIO;
using BigBang1112.WorldRecordReportLib.Services.Wrappers;

namespace BigBang1112.WorldRecordReportLib.Jobs;

[DisallowConcurrentExecution]
public class AcquireNewOfficialCampaignsJob : IJob
{
    private readonly IWrUnitOfWork _wrUnitOfWork;
    private readonly ITrackmaniaIoApiService _tmIo;
    private readonly HttpClient _http;
    private readonly ILogger<AcquireNewOfficialCampaignsJob> _logger;

    public AcquireNewOfficialCampaignsJob(IWrUnitOfWork wrUnitOfWork, ITrackmaniaIoApiService tmIo, HttpClient http, ILogger<AcquireNewOfficialCampaignsJob> logger)
    {
        _wrUnitOfWork = wrUnitOfWork;
        _tmIo = tmIo;
        _http = http;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await AcquireNewOfficialCampaignsAsync();
    }

    internal async Task AcquireNewOfficialCampaignsAsync(int delay = 500, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Acquiring map data about official campaigns...");

        var campaigns = await _tmIo.GetCampaignsAsync(cancellationToken: cancellationToken);

        _logger.LogInformation("{count} campaigns found.", campaigns.Count);

        var game = await _wrUnitOfWork.Games.GetAsync(Game.TM2020, cancellationToken);
        var env = await _wrUnitOfWork.Envs.GetAsync(Env.Stadium2020, cancellationToken);
        var mode = await _wrUnitOfWork.MapModes.GetAsync(MapMode.Race, cancellationToken);

        foreach (var campaign in campaigns)
        {
            if (campaign is not OfficialCampaignItem officialCampaign)
            {
                _logger.LogInformation("End of official campaigns.");
                break;
            }

            await ProcessOfficialCampaignAsync(officialCampaign, game, env, mode, delay, cancellationToken);
        }
    }

    internal async Task ProcessOfficialCampaignAsync(OfficialCampaignItem officialCampaign,
                                                     GameModel game,
                                                     EnvModel env,
                                                     MapModeModel mode,
                                                     int delay,
                                                     CancellationToken cancellationToken)
    {
        var details = await _tmIo.GetOfficialCampaignAsync(officialCampaign.Id, cancellationToken);
        
        _logger.LogInformation("{name} campaign details fetched.", details.Name);

        var campaignModel = await _wrUnitOfWork.Campaigns.GetOrAddAsync<CampaignModel>(x => x.LeaderboardUid == details.LeaderboardUid, () => new CampaignModel
        {
            Game = game,
            LeaderboardUid = details.LeaderboardUid,
            Name = details.Name
        }, cancellationToken);

        if (campaignModel.IsOver)
        {
            _logger.LogInformation("{name} campaign no longer receives updates.", details.Name);
            return;
        }

        var loginDictionary = new Dictionary<Guid, LoginModel>();

        foreach (var map in details.Playlist)
        {
            if (!loginDictionary.TryGetValue(map.Author, out LoginModel? loginModel))
            {
                loginModel = await _wrUnitOfWork.Logins.GetOrAddAsync(game, map.Author.ToString(), map.AuthorPlayer.Name, cancellationToken);
                loginDictionary.Add(map.Author, loginModel);

                _logger.LogInformation("Login model of '{nickname}' ({name}) received.", loginModel.Nickname, loginModel.Name);
            }

            var mapModel = await _wrUnitOfWork.Maps.GetOrAddAsync<MapModel>(x => string.Equals(x.MapUid, map.MapUid), () => new MapModel
            {
                MapUid = map.MapUid,
                Game = game,
                Environment = env,
                Name = map.Name,
                DeformattedName = map.Name,
                Author = loginModel,
                MxId = map.ExchangeId,
                Mode = mode,
                MapType = map.MapType,
                MapStyle = map.MapStyle,
                ThumbnailGuid = new Guid(new Uri(map.ThumbnailUrl).Segments.Last()[..36]),
                DownloadGuid = new Guid(new Uri(map.FileUrl).Segments.Last()),
                Campaign = campaignModel,
                AddedOn = DateTime.UtcNow,
                UpdatedOn = DateTime.UtcNow,
            }, cancellationToken);

            _logger.LogInformation("{name} map model retrieved/created. Checking the map file...", mapModel.DeformattedName);
            
            var mapModelChanged = await CheckMapDataAsync(map, mapModel, cancellationToken);

            if (mapModelChanged)
            {
                await _wrUnitOfWork.SaveAsync(cancellationToken);
            }

            await Task.Delay(delay, cancellationToken);
        }
    }

    internal async Task<bool> CheckMapDataAsync(Map map, MapModel mapModel, CancellationToken cancellationToken)
    {
        if (mapModel.FileLastModifiedOn is null || mapModel.MapId is null)
        {
            return await ProcessMapDataAsync(map, mapModel, cancellationToken);
        }
        
        using var headResponse = await _http.HeadAsync(map.FileUrl);

        if (!headResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("{name} map file head check failed ({code}, {url}).", mapModel.DeformattedName, headResponse.StatusCode, map.FileUrl);
            return false;
        }

        _logger.LogInformation("{name} map file head check successful.", mapModel.DeformattedName);

        if (headResponse.Content.Headers.LastModified.HasValue)
        {
            var lastModified = headResponse.Content.Headers.LastModified.Value;

            if (lastModified.UtcDateTime > mapModel.FileLastModifiedOn)
            {
                return await ProcessMapDataAsync(map, mapModel, cancellationToken);
            }
        }

        return false;
    }

    internal async Task<bool> ProcessMapDataAsync(Map map, MapModel mapModel, CancellationToken cancellationToken)
    {
        using var response = await _http.GetAsync(map.FileUrl, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("{name} map file download failed ({code}, {url}).", mapModel.DeformattedName, response.StatusCode, map.FileUrl);
            return false;
        }

        _logger.LogInformation("{name} map file download successful. Calculating checksum...", mapModel.DeformattedName);

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var sha256 = SHA256.Create();

        mapModel.Checksum = sha256.ComputeHash(stream);

        _logger.LogInformation("{name} map file checksum: {checksum}", mapModel.DeformattedName, BitConverter.ToString(mapModel.Checksum));

        if (response.Content.Headers.LastModified.HasValue)
        {
            mapModel.FileLastModifiedOn = response.Content.Headers.LastModified.Value.UtcDateTime;
            _logger.LogInformation("{name} map file last modified on: {lastModified}", mapModel.DeformattedName, mapModel.FileLastModifiedOn);
        }
        else
        {
            _logger.LogWarning("{name} map file last modified on not found.", mapModel.DeformattedName);
        }

        mapModel.MapId = map.MapId;

        return true;
    }
}
