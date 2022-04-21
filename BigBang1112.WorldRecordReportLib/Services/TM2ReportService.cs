using BigBang1112.Services;
using BigBang1112.WorldRecordReportLib.Data;
using BigBang1112.WorldRecordReportLib.Exceptions;
using BigBang1112.WorldRecordReportLib.Models;
using BigBang1112.WorldRecordReportLib.Models.Db;
using Humanizer.Bytes;
using Mapster;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics.CodeAnalysis;
using TmEssentials;
using TmXmlRpc;
using TmXmlRpc.Requests;
using Microsoft.Extensions.Logging;
using BigBang1112.WorldRecordReportLib.Repos;

namespace BigBang1112.WorldRecordReportLib.Services;

public class TM2ReportService : ITM2ReportService
{
    private static readonly MasterServerTm2 server = new();

    private static readonly Discord.Embed bigbangEmbed = new Discord.EmbedBuilder()
        .WithDescription("I wish I could drive world records again but this one unfortunately does not belong to me. :disappointed:")
        .Build();

    private static readonly Discord.Embed technoEmbed = new Discord.EmbedBuilder()
        .WithDescription("I prefer Drum & Bass TBH :ok_hand::joy::thumbsup:")
        .Build();

    private readonly ILogger<TM2ReportService> _logger;
    private readonly IWrRepo _repo;
    private readonly IRecordSetService _recordSetService;
    private readonly IMemoryCache _cache;
    private readonly IDiscordWebhookService _discordWebhookService;
    private readonly IGhostService _ghostService;

    public TM2ReportService(
        ILogger<TM2ReportService> logger,
        IWrRepo repo,
        IRecordSetService recordSetService,
        IMemoryCache cache,
        IDiscordWebhookService discordWebhookService,
        IGhostService ghostService)
    {
        _logger = logger;
        _repo = repo;
        _recordSetService = recordSetService;
        _cache = cache;
        _discordWebhookService = discordWebhookService;
        _ghostService = ghostService;
    }

    /// <exception cref="RefreshLoopNotFoundException"/>
    /// <exception cref="MapGroupNotFoundException"/>
    public async Task RefreshWorldRecordsAsync(DateTime fireTime)
    {
        var logPrefix = $"[Refresh {fireTime}] ";

        var mapGroup = await GetNextMapGroupAsync(fireTime);
        var maps = await GetMapsFromMapGroupToDictionaryAsync(mapGroup);

        SetTitlePack(mapGroup);

        _logger.LogInformation(logPrefix + $"Fetching {maps.Count} maps [{mapGroup}]");

        var leaderboardsTask = GetLeaderboardsFromMapsAsync(fireTime, maps: GetMapsForRequest(maps.Keys));

        var wrs = await GetWorldRecordHistoryFromMapGroupAsync(mapGroup);

        var leaderboards = await leaderboardsTask;

        _logger.LogInformation(logPrefix + $"Finished in {leaderboards.ExecutionTime.TotalSeconds}s ({(leaderboards.ByteSize.HasValue ? ByteSize.FromBytes(leaderboards.ByteSize.Value) : "unknown size")})");

        var newWrsToReport = new List<WorldRecordModel>();
        var removedWrsToReport = new List<RemovedWorldRecord>();
        var nicknameDictionary = new Dictionary<string, string>();

        foreach (var leaderboard in leaderboards) // TODO: run in parallel
        {
            var map = maps[leaderboard.MapUid];
            var mappedLeaderboard = leaderboard.Adapt<Leaderboard>();

            await UpdateRecordSetAsync(leaderboard, nicknameDictionary);
            await HandleLeaderboardFromMapDictionaryAsync(map, wrs, mappedLeaderboard,
                newWrsToReport, removedWrsToReport);
        }

        await UpdateNicknamesAsync(await _repo.GetTM2GameAsync(), nicknameDictionary);
        await ReportChangesAsync(newWrsToReport, removedWrsToReport);

        await _repo.SaveAsync();
    }

    public async Task HandleLeaderboardAsync(string mapUid, Leaderboard leaderboard, bool isManialinkReport)
    {
        var map = await _repo.GetMapByUidAsync(mapUid);

        if (map is null)
        {
            throw new Exception();
        }

        await HandleLeaderboardAsync(map, leaderboard, isManialinkReport);
    }

    public async Task HandleLeaderboardAsync(MapModel map, Leaderboard leaderboard, bool isManialinkReport)
    {
        var newWrsToReport = new List<WorldRecordModel>();
        var removedWrsToReport = new List<RemovedWorldRecord>();
        await HandleLeaderboardAsync(map, leaderboard, newWrsToReport, removedWrsToReport, isManialinkReport);
        await ReportChangesAsync(newWrsToReport, removedWrsToReport);
        await _repo.SaveAsync();
    }

    public async Task HandleLeaderboardAsync(MapModel map, Leaderboard leaderboard,
        List<WorldRecordModel> newWrsToReport, List<RemovedWorldRecord> removedWrsToReport, bool isManialinkReport)
    {
        var wrs = await _repo.GetWorldRecordHistoryFromMapAsync(map);
        var currentWr = wrs.FirstOrDefault(x => !x.Ignored);
        var ignoredRecords = wrs.Where(x => x.Ignored);

        await HandleLeaderboardAsync(map, leaderboard, currentWr, ignoredRecords,
            newWrsToReport, removedWrsToReport, isManialinkReport);
    }

    private async Task HandleLeaderboardAsync(MapModel map, Leaderboard leaderboard, WorldRecordModel? currentWr, IEnumerable<WorldRecordModel> ignoredRecords,
        List<WorldRecordModel> newWrsToReport, List<RemovedWorldRecord> removedWrsToReport, bool isManialinkReport)
    {
        foreach (var record in leaderboard.Records)
        {
            if (IsInvalidRecord(record, ignoredRecords))
            {
                continue;
            }

            if (IsNewWorldRecord(currentWr, record))
            {
                var newWr = await AddNewWorldRecordAsync(map, record, previousWr: currentWr);

                if (newWr is null)
                    break;

                newWrsToReport.Add(newWr);

                break;
            }

            if (!isManialinkReport && !currentWr.Unverified)
            {
                if (IsRemovedWorldRecord(currentWr, record))
                {
                    var removedWr = await RemoveWorldRecordAsync(map, currentWr, record);

                    removedWrsToReport.Add(removedWr);

                    break;
                }
            }

            // The time is automatically illegal/equal/worse, therefore no further check needed

            break;
        }
    }

    /// <exception cref="RefreshLoopNotFoundException"/>
    /// <exception cref="MapGroupNotFoundException"/>
    private async Task<MapGroupModel> GetNextMapGroupAsync(DateTime fireTimeUtc)
    {
        var lastRefresh = _cache.Get<RefreshModel>(CacheKeys.RefreshTM2OfficialCurrent);
        var currentRefresh = await GetCurrentRefreshAsync(lastRefresh);

        _cache.Set(CacheKeys.RefreshTM2OfficialCurrent, currentRefresh);

        currentRefresh.OccurredOn = fireTimeUtc;

        await _repo.SaveAsync();

        if (currentRefresh is null)
        {
            throw new Exception("Illegal");
        }

        var mapGroup = currentRefresh.MapGroup;

        if (mapGroup is null)
        {
            throw new MapGroupNotFoundException();
        }

        return mapGroup;
    }

    /// <exception cref="RefreshLoopNotFoundException"/>
    private async Task<RefreshModel> GetCurrentRefreshAsync(RefreshModel lastRefresh)
    {
        var currentRefresh = default(RefreshModel);

        if (lastRefresh is not null)
        {
            currentRefresh = await _repo.GetRefreshByIdAsync(lastRefresh.Id);
        }

        if (currentRefresh is null || currentRefresh.NextRefresh is null)
        {
            var guid = OfficialGuids.RefreshTM2Official;

            var refreshLoop = await _repo.GetRefreshLoopByGuidAsync(guid);

            if (refreshLoop is null)
            {
                throw new RefreshLoopNotFoundException(guid);
            }

            return refreshLoop.StartingRefresh;
        }

        return currentRefresh.NextRefresh;
    }

    private static void SetTitlePack(MapGroupModel mapGroup)
    {
        var titlePack = mapGroup.TitlePack;

        if (titlePack is null)
            throw new Exception("Title pack definition is missing and required for this refresh loop.");

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
        GetLeaderboardsFromMapsAsync(DateTimeOffset date,
            IList<GetMapLeaderBoardSummaries<RequestGameManiaPlanet>.Map> maps)
    {
        MasterServer.Client.DefaultRequestHeaders.Date = date; //
        var task = server.GetMapLeaderBoardSummariesAsync(maps);
        MasterServer.Client.DefaultRequestHeaders.Date = null;
        return await task;
    }

    private async Task<Dictionary<string, MapModel>> GetMapsFromMapGroupToDictionaryAsync(MapGroupModel mapGroup)
    {
        var maps = await _repo.GetMapsFromMapGroupAsync(mapGroup);
        return maps.ToDictionary(x => x.MapUid);
    }

    private async Task UpdateNicknamesAsync(GameModel game, Dictionary<string, string> nicknameDictionary)
    {
        var logins = await _repo.GetLoginsInTM2Async();

        foreach (var (login, nickname) in nicknameDictionary)
        {
            var loginModel = logins.FirstOrDefault(x => x.Name == login);

            if (loginModel is null)
            {
                _ = await _repo.GetOrAddLoginAsync(login, nickname, game);

                continue;
            }

            if (loginModel.Nickname is not null && loginModel.Nickname != nickname)
            {
                var latestNicknameChange = await _repo.GetLatestNicknameChangeByLoginAsync(loginModel);

                if (latestNicknameChange is not null
                    && DateTime.UtcNow - latestNicknameChange.PreviousLastSeenOn <= TimeSpan.FromHours(1))
                {
                    continue; // loginModel.Nickname set MUST be skipped, otherwise the change would get lost
                }

                // Track nickname change
                var nicknameChangeModel = new NicknameChangeModel
                {
                    Login = loginModel,
                    Previous = loginModel.Nickname,
                    PreviousLastSeenOn = DateTime.UtcNow
                };

                await _repo.AddNicknameChangeAsync(nicknameChangeModel);
            }

            loginModel.Nickname = nickname;
        }
    }

    private async Task ReportChangesAsync(List<WorldRecordModel> newWrsToReport, List<RemovedWorldRecord> removedWrsToReport)
    {
        foreach (var removedWr in removedWrsToReport)
        {
            await ReportRemovedWorldRecordAsync(removedWr);
        }

        foreach (var wr in newWrsToReport)
        {
            await ReportNewWorldRecordAsync(wr);
        }
    }

    private async Task ReportNewWorldRecordAsync(WorldRecordModel wr)
    {
        var report = new ReportModel
        {
            Guid = Guid.NewGuid(),
            Type = ReportModel.EType.NewWorldRecord,
            HappenedOn = DateTime.UtcNow,
            WorldRecord = wr
        };

        await _repo.AddReportAsync(report);

        var embed = _discordWebhookService.GetDefaultEmbed_NewWorldRecord(wr);

        await SendMessageToAllWebhooksAsync(embed, report);
    }

    private async Task ReportRemovedWorldRecordAsync(RemovedWorldRecord removedWr)
    {
        var report = new ReportModel
        {
            Guid = Guid.NewGuid(),
            Type = ReportModel.EType.RemovedWorldRecord,
            HappenedOn = DateTime.UtcNow,
            WorldRecord = removedWr.Current,
            RemovedWorldRecord = removedWr.Previous
        };

        await _repo.AddReportAsync(report);

        var embed = _discordWebhookService.GetDefaultEmbed_RemovedWorldRecord(removedWr);

        await SendMessageToAllWebhooksAsync(embed, report);
    }

    private async Task HandleLeaderboardFromMapDictionaryAsync(
        MapModel map,
        Dictionary<string, IGrouping<MapModel, WorldRecordModel>> wrs,
        Leaderboard leaderboard,
        List<WorldRecordModel> newWrsToReport,
        List<RemovedWorldRecord> removedWrsToReport)
    {
        GetSpecificsFromMapUid(wrs, map.MapUid,
            out WorldRecordModel? currentWr,
            out IEnumerable<WorldRecordModel> ignoredRecords,
            out IEnumerable<WorldRecordModel> unverifiedRecords);
        await HandleLeaderboardAsync(map, leaderboard, currentWr, ignoredRecords,
            newWrsToReport, removedWrsToReport, isManialinkReport: false);
        await VerifyUnverifiedRecordsAsync(leaderboard, unverifiedRecords);
    }

    private async Task VerifyUnverifiedRecordsAsync(Leaderboard leaderboard, IEnumerable<WorldRecordModel> unverifiedRecords)
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
                    timestamp = await _ghostService.DownloadGhostAndGetTimestampAsync(leaderboard.MapUid, record);
                    login = record.Login;
                    nickname = record.Nickname;

                    break;
                }
            }

            if (verified)
            {
                unverifiedWr.ReplayUrl = replayUrl;
                unverifiedWr.Unverified = false;
                unverifiedWr.DrivenOn = timestamp.UtcDateTime;
                unverifiedWr.PublishedOn = timestamp.UtcDateTime;
                unverifiedWr.Player.Name = login!;
                unverifiedWr.Player.Nickname = nickname;

                var report = await _repo.GetReportFromWorldRecordAsync(unverifiedWr);

                if (report is null)
                {
                    return;
                }

                var embed = _discordWebhookService.GetDefaultEmbed_NewWorldRecord(unverifiedWr);
                var embeds = new List<Discord.Embed> { embed };
                embeds.AddRange(ApplyAdditionalEmbeds(login!, nickname!));

                foreach (var msg in report.DiscordWebhookMessages)
                {
                    using var client = _discordWebhookService.CreateWebhookClient(msg.Webhook.Url, out bool isDeleted);

                    if (client is null)
                    {
                        continue;
                    }

                    await client.ModifyMessageAsync(msg.MessageId, func => func.Embeds = embeds);
                }
            }
            else
            {
                // Further checking with about 2 hour tolerance
            }
        }
    }

    private async Task<RemovedWorldRecord> RemoveWorldRecordAsync(MapModel map, WorldRecordModel currentWr, LeaderboardRecord record)
    {
        // The record was removed from the game leaderboards
        // Record in the database should be marked as ignored
        // Do not report

        currentWr.Ignored = true;

        var prevWr = currentWr.PreviousWorldRecord;

        var newWrInReports = default(WorldRecordModel);
        var ignoredPrevWrList = new List<WorldRecordModel>();

        while (prevWr is not null)
        {
            if (record.Time == prevWr.TimeInt32
             && record.Login == prevWr.Player?.Name)
            {
                newWrInReports = prevWr;
                break;
            }

            ignoredPrevWrList.Add(prevWr);

            prevWr = prevWr.PreviousWorldRecord;
        }

        if (newWrInReports is not null)
        {
            // Now the WR is one of the previous WRs, do nothing more

            foreach (var wr in ignoredPrevWrList)
            {
                wr.Ignored = true;
            }

            return new RemovedWorldRecord(Previous: currentWr, Current: newWrInReports);
        }

        // The WR is a fresh WR, add a new wr
        // Reference the PREVIOUS WR from 'currentWr' as the previous wr
        // It is possible to be null if 'currentWr.PreviousWorldRecord' is null

        var timestamp = await _ghostService.DownloadGhostAndGetTimestampAsync(map.MapUid, record);

        var freshWr = await CreateWorldRecordAsync(record, map, timestamp,
            previousWr: currentWr.PreviousWorldRecord,
            publishedTimestamp: DateTime.UtcNow);

        await _repo.AddWorldRecordAsync(freshWr);

        return new RemovedWorldRecord(Previous: currentWr, Current: freshWr);
    }

    private async Task<WorldRecordModel?> AddNewWorldRecordAsync(MapModel map, LeaderboardRecord record, WorldRecordModel? previousWr)
    {
        var timestamp = await _ghostService.DownloadGhostAndGetTimestampAsync(map.MapUid, record);

        if (previousWr is null)
        {
            // First record on this map

            var wr = await CreateWorldRecordAsync(record, map, timestamp, previousWr);
            await _repo.AddWorldRecordAsync(wr);
            return wr;
        }

        // Better time detected

        if (timestamp.UtcDateTime > previousWr.PublishedOn)
        {
            // Normal improvement

            var wr = await CreateWorldRecordAsync(record, map, timestamp, previousWr);
            await _repo.AddWorldRecordAsync(wr);
            return wr;
        }

        // New WR that appeared
        // An older time caused by login ignore
        // Do not report

        return null;
    }

    private static bool IsRemovedWorldRecord(WorldRecordModel currentWr, LeaderboardRecord record)
    {
        return record.Rank == 1 && record.Time > currentWr.TimeInt32;
    }

    /// <summary>
    /// Either if there's no WR, or it is faster than a known WR.
    /// </summary>
    /// <param name="currentWr">Current world record in the database.</param>
    /// <param name="record">Record from the leaderboard.</param>
    /// <returns>If <paramref name="record"/> is a new world record.</returns>
    private static bool IsNewWorldRecord([NotNullWhen(false)] WorldRecordModel? currentWr, LeaderboardRecord record)
    {
        return currentWr is null || record.Time < currentWr.TimeInt32;
    }

    private static bool IsInvalidRecord(LeaderboardRecord record, IEnumerable<WorldRecordModel> ignoredRecords)
    {
        // If time is not legit
        if (record.Time <= TimeInt32.Zero)
        {
            return true;
        }

        if (IsIgnoredRecord(ignoredRecords, record))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// If there's an ignored record that is equal to this <paramref name="record"/>.
    /// </summary>
    /// <param name="ignoredRecords">List of ignored world records from the database.</param>
    /// <param name="record">Record from the leaderboard.</param>
    /// <returns></returns>
    private static bool IsIgnoredRecord(IEnumerable<WorldRecordModel> ignoredRecords, LeaderboardRecord record)
    {
        return ignoredRecords.SingleOrDefault(ignoredRec => ignoredRec is not null
            && ignoredRec.TimeInt32 == record.Time && ignoredRec.Player?.Name == record.Login)
            is not null;
    }

    private static void GetSpecificsFromMapUid(Dictionary<string, IGrouping<MapModel, WorldRecordModel>> wrs,
        string mapUid, out WorldRecordModel? currentWr, out IEnumerable<WorldRecordModel> ignoredRecords,
        out IEnumerable<WorldRecordModel> unverifiedRecords)
    {
        // If wrs exist
        if (wrs.TryGetValue(mapUid, out IGrouping<MapModel, WorldRecordModel>? wrHistory))
        {
            currentWr = wrHistory.FirstOrDefault(x => !x.Ignored);
            ignoredRecords = wrHistory.Where(x => x.Ignored);
            unverifiedRecords = wrHistory.Where(x => x.Unverified);

            return;
        }

        currentWr = null;
        ignoredRecords = Enumerable.Empty<WorldRecordModel>();
        unverifiedRecords = Enumerable.Empty<WorldRecordModel>();
    }

    private async Task UpdateRecordSetAsync(MapLeaderBoard lb, Dictionary<string, string> nicknameDictionary)
    {
        var recordList = new List<RecordSetDetailedRecord>(lb.Records.Count);
        
        foreach (var record in lb.Records)
        {
            nicknameDictionary[record.Login] = record.Nickname;

            recordList.Add(new RecordSetDetailedRecord(record.Rank, record.Login, record.Time.TotalMilliseconds, record.ReplayUrl));
        }

        var recordSet = new RecordSet(recordList, lb.Times);

        await _recordSetService.UpdateRecordSetAsync(lb.Zone, lb.MapUid, recordSet, nicknameDictionary);
    }

    private async Task<Dictionary<string, IGrouping<MapModel, WorldRecordModel>>>
        GetWorldRecordHistoryFromMapGroupAsync(MapGroupModel mapGroup)
    {
        var relevantWrs = await _repo.GetWorldRecordHistoryFromMapGroupAsync(mapGroup);

        var wrs = relevantWrs
            .GroupBy(x => x.Map)
            .ToDictionary(x => x.Key.MapUid);

        return wrs;
    }

    private async Task SendMessageToAllWebhooksAsync(Discord.Embed embed, ReportModel report)
    {
        var lbManialinkEmbed = new Discord.EmbedBuilder()
            .WithFooter("This report was sent early using the Leaderboards manialink and will be verified within an hour.")
            .Build();

        var embeds = new List<Discord.Embed> { embed };

        if (report.WorldRecord is null)
        {
            return;
        }

        var login = report.WorldRecord.GetPlayerLogin();
        var nickname = report.WorldRecord.GetPlayerNicknameDeformatted().EscapeDiscord();

        embeds.AddRange(ApplyAdditionalEmbeds(login, nickname));

        if (report.WorldRecord?.Unverified == true)
        {
            embeds.Add(lbManialinkEmbed);
        }

        foreach (var webhook in await _repo.GetDiscordWebhooksAsync())
        {
            if (!Uri.IsWellFormedUriString(webhook.Url, UriKind.Absolute))
                continue;

            if (string.IsNullOrWhiteSpace(webhook.Filter))
            {
                // Allows everything
            }
            else
            {
                try
                {
                    var filter = JsonHelper.Deserialize<DiscordWebhookFilter>(webhook.Filter);

                    if (filter.ReportTM2 is null)
                    {
                        continue;
                    }
                    else
                    {
                        if (report.WorldRecord is null)
                        {
                            continue;
                        }

                        var map = report.WorldRecord.Map;

                        if (map.TitlePack is null) continue;

                        // If the title pack of the map is not in the filter list
                        if (!filter.ReportTM2
                            .Select(x => x.TitleId)
                            .Contains(map.TitlePack.GetTitleUid()))
                            continue;
                    }
                }
                catch
                {
                    continue;
                }
            }

            var message = await _discordWebhookService.SendMessageAsync(webhook, snowflake => new DiscordWebhookMessageModel
            {
                MessageId = snowflake,
                Report = report,
                SentOn = DateTime.UtcNow,
                ModifiedOn = DateTime.UtcNow,
                Webhook = webhook
            }, embeds: embeds);
        }
    }

    private static IEnumerable<Discord.Embed> ApplyAdditionalEmbeds(string login, string nickname)
    {
        var rioluEmbedBuilder = new Discord.EmbedBuilder()
            .WithDescription("Look at this comedy genius! This world record holder has **riolu** in their current nickname!\nHahah what a funny gamer :joy: :joy: :joy:");

        if (nickname?.Contains("riolu", StringComparison.OrdinalIgnoreCase) == true && login != "riolu")
        {
            if (nickname?.Contains("riolu") == false)
            {
                rioluEmbedBuilder.Description += " imagine avoiding this filter by casing, I am not that stupid (but I was)";
            }

            yield return rioluEmbedBuilder.Build();
        }

        if (nickname?.Contains("r¡olu", StringComparison.OrdinalIgnoreCase) == true && login != "riolu")
        {
            rioluEmbedBuilder.Description += " nice character there";

            yield return rioluEmbedBuilder.Build();
        }

        if (nickname?.Contains("bigbang", StringComparison.OrdinalIgnoreCase) == true && login != "bigbang1112")
        {
            yield return bigbangEmbed;
        }

        if (nickname?.Contains("techno", StringComparison.OrdinalIgnoreCase) == true && login != "techno")
        {
            yield return technoEmbed;
        }
    }

    private async Task<WorldRecordModel> CreateWorldRecordAsync(LeaderboardRecord record,
        MapModel map, DateTimeOffset recordTimestamp, WorldRecordModel? previousWr,
        DateTime? publishedTimestamp = null)
    {
        var login = await _repo.GetOrAddLoginAsync(record.Login, record.Nickname, await _repo.GetTM2GameAsync());

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
