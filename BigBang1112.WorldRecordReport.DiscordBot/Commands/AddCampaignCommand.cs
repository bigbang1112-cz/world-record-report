using System.Security.Cryptography;
using BigBang1112.DiscordBot;
using BigBang1112.DiscordBot.Attributes;
using BigBang1112.DiscordBot.Models;
using BigBang1112.WorldRecordReportLib.Data;
using BigBang1112.WorldRecordReportLib.Enums;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Services.Wrappers;
using Discord;
using Discord.WebSocket;
using ManiaAPI.TrackmaniaIO;
using Microsoft.Extensions.Logging;
using TmEssentials;
using Game = BigBang1112.WorldRecordReportLib.Enums.Game;

namespace BigBang1112.WorldRecordReport.DiscordBot.Commands;

[DiscordBotCommand("addcampaign", "Adds TM2020 campaign to the map set.")]
public class AddCampaignCommand : DiscordBotCommand
{
    private readonly IWrUnitOfWork _wrUnitOfWork;
    private readonly ITrackmaniaIoApiService _trackmaniaIoApiService;
    private readonly ILogger<AddCampaignCommand> _logger;
    private readonly HttpClient _http;

    [DiscordBotCommandOption("clubid", ApplicationCommandOptionType.Integer, "Club ID.", IsRequired = true)]
    public long ClubId { get; set; }

    [DiscordBotCommandOption("campaignid", ApplicationCommandOptionType.Integer, "Campaign ID.", IsRequired = true)]
    public long CampaignId { get; set; }
    
    public AddCampaignCommand(DiscordBotService discordBotService,
                              IWrUnitOfWork wrUnitOfWork,
                              ITrackmaniaIoApiService trackmaniaIoApiService,
                              HttpClient http,
                              ILogger<AddCampaignCommand> logger) : base(discordBotService)
    {
        _wrUnitOfWork = wrUnitOfWork;
        _trackmaniaIoApiService = trackmaniaIoApiService;
        _logger = logger;
        _http = http;
    }

    public override async Task<DiscordBotMessage> ExecuteAsync(SocketInteraction slashCommand, Deferer deferer)
    {
        var campaign = await _trackmaniaIoApiService.GetCustomCampaignAsync((int)ClubId, (int)CampaignId);

        await deferer.DeferAsync(ephemeral: true);

        var game = await _wrUnitOfWork.Games.GetAsync(Game.TM2020);
        var env = await _wrUnitOfWork.Envs.GetAsync(Env.Stadium2020);
        var mode = await _wrUnitOfWork.MapModes.GetAsync(MapMode.Race);
        
        var campaignModel = await _wrUnitOfWork.Campaigns.GetOrAddAsync(x => x.LeaderboardUid == campaign.LeaderboardUid, () => new CampaignModel
        {
            Game = game,
            LeaderboardUid = campaign.LeaderboardUid,
            Name = campaign.Name,
            PublishedOn = campaign.PublishTime.UtcDateTime
        });

        var loginDictionary = new Dictionary<Guid, LoginModel>();

        foreach (var map in campaign.Playlist)
        {
            if (!loginDictionary.TryGetValue(map.Author, out LoginModel? loginModel))
            {
                loginModel = await _wrUnitOfWork.Logins.GetOrAddAsync(game, map.Author.ToString(), map.AuthorPlayer.Name);
                loginDictionary.Add(map.Author, loginModel);

                _logger.LogInformation("Login model of '{nickname}' ({name}) received.", loginModel.Nickname, loginModel.Name);
            }

            var mapModel = await _wrUnitOfWork.Maps.GetOrAddAsync(x => string.Equals(x.MapUid, map.MapUid), () => new MapModel
            {
                MapUid = map.MapUid,
                Game = game,
                Environment = env,
                Name = map.Name,
                DeformattedName = TextFormatter.Deformat(map.Name),
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
                MapId = map.MapId
            });

            _logger.LogInformation("{name} map model retrieved/created. Checking the map file...", mapModel.DeformattedName);

            var mapModelChanged = await CheckMapDataAsync(map, mapModel);

            if (mapModelChanged)
            {
                await _wrUnitOfWork.SaveAsync();
            }

            await Task.Delay(500);
        }

        return new DiscordBotMessage("Campaign added.");
    }

    internal async Task<bool> CheckMapDataAsync(Map map, MapModel mapModel)
    {
        if (mapModel.FileLastModifiedOn is null || mapModel.MapId is null)
        {
            return await ProcessMapDataAsync(map, mapModel);
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
                return await ProcessMapDataAsync(map, mapModel);
            }
        }

        return false;
    }

    internal async Task<bool> ProcessMapDataAsync(Map map, MapModel mapModel)
    {
        using var response = await _http.GetAsync(map.FileUrl);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("{name} map file download failed ({code}, {url}).", mapModel.DeformattedName, response.StatusCode, map.FileUrl);
            return false;
        }

        _logger.LogInformation("{name} map file download successful. Calculating checksum...", mapModel.DeformattedName);

        using var stream = await response.Content.ReadAsStreamAsync();
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
