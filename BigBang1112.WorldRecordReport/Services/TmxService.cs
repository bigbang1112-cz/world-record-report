using BigBang1112.WorldRecordReport.Data;
using BigBang1112.WorldRecordReport.Models;
using BigBang1112.WorldRecordReport.Models.Db;
using TmExchangeApi;
using TmExchangeApi.Models;

namespace BigBang1112.WorldRecordReport.Services;

public class TmxService
{
    private readonly IWrRepo _repo;
    private readonly IDiscordWebhookService _discordWebhookService;
    private readonly ILogger<TmxService> _logger;

    public TmxService(IWrRepo repo, IDiscordWebhookService discordWebhookService, ILogger<TmxService> logger)
    {
        _repo = repo;
        _discordWebhookService = discordWebhookService;
        _logger = logger;
    }

    public async Task CleanupRemovedWorldRecordsAsync()
    {
        var lastTmxWrs = await _repo.GetLastWorldRecordsInTMUFAsync(5);

        await CleanupWorldRecordsAsync(lastTmxWrs, false);

        async Task CleanupWorldRecordsAsync(IEnumerable<WorldRecordModel> lastTmxWrs, bool onlyOneUser)
        {
            foreach (var wr in lastTmxWrs)
            {
                if (wr.TmxPlayer is null)
                {
                    continue;
                }

                var site = wr.TmxPlayer.Site;

                var map = wr.Map;

                if (!map.MxId.HasValue)
                    throw new Exception();

                var tmxId = map.MxId.Value;

                var siteEnum = site.ShortName switch
                {
                    NameConsts.TMXSiteTMNF => TmxSite.TMNForever,
                    NameConsts.TMXSiteUnited => TmxSite.United,
                    _ => throw new Exception(),
                };

                var tmx = new Tmx(siteEnum);

                var replays = await tmx.GetReplaysAsync(tmxId);

                if (map.Mode?.Name == NameConsts.MapModeStunts)
                {
                    // TODO
                    continue;
                }

                var tmxWrs = GetWrHistory(replays).Reverse();
                var tmxWr = tmxWrs.FirstOrDefault();

                if (tmxWr is not null && tmxWr.ReplayTime <= wr.Time)
                {
                    continue;
                }

                // The record was removed and there are no more records
                // or the TMX record is slower than WR in the database

                wr.Ignored = true;

                var report = await _repo.GetReportFromWorldRecordAsync(wr);

                if (report is null)
                    throw new Exception();

                foreach (var msg in report.DiscordWebhookMessages)
                {
                    using var webhookClient = new Discord.Webhook.DiscordWebhookClient(msg.Webhook.Url);

                    try
                    {
                        await webhookClient.DeleteMessageAsync(msg.MessageId);
                        msg.RemovedOfficially = true;
                    }
                    catch (Discord.Net.HttpException httpEx)
                    {
                        if (httpEx.HttpCode == System.Net.HttpStatusCode.NotFound
                        && !msg.RemovedOfficially)
                        {
                            msg.RemovedByUser = true;
                        }
                    }
                }

                if (!onlyOneUser)
                {
                    await _repo.SaveAsync();

                    var otherWrsByThisUser = await _repo.GetWorldRecordsByTmxPlayerAsync(wr.TmxPlayer);

                    if (otherWrsByThisUser.Count > 0)
                    {
                        await CleanupWorldRecordsAsync(otherWrsByThisUser, true);
                    }
                }

                await _repo.SaveAsync();
            }
        }
    }

    public async Task UpdateWorldRecordsAsync(TmxSite site, LeaderboardType leaderboardType)
    {
        var tmx = new Tmx(site);

        var tmxSite = site switch
        {
            TmxSite.United => await _repo.GetUnitedTmxAsync(),
            TmxSite.TMNForever => await _repo.GetTMNFTmxAsync(),
            _ => throw new Exception()
        };

        var endOfNewActivities = false;

        var recentTracks = default(ItemCollection<TrackSearchItem>);

        do
        {
            _logger.LogInformation("Searching Nadeo United maps, most recent activity...");

            recentTracks = await tmx.SearchAsync(new TrackSearchFilters
            {
                PrimaryOrder = TrackOrder.ActivityMostRecent,
                LeaderboardType = leaderboardType,
                AfterTrackId = recentTracks?.Results.LastOrDefault()?.TrackId
            });

            _logger.LogInformation("{count} maps found.", recentTracks.Results.Length);

            foreach (var tmxTrack in recentTracks.Results)
            {
                var map = await _repo.GetMapByMxIdAsync(tmxTrack.TrackId, tmxSite);

                if (map is null)
                {
                    continue;
                }

                if (map.LastActivityOn is not null && tmxTrack.ActivityAt.UtcTicks - (tmxTrack.ActivityAt.UtcTicks % TimeSpan.TicksPerSecond) <= map.LastActivityOn.Value.Ticks)
                {
                    endOfNewActivities = true;
                    break;
                }

                var freshUpdate = false;

                if (map.LastActivityOn is null)
                {
                    _logger.LogInformation("Fresh activity, world records are not going to be reported. {mapname}", map.DeformattedName);
                    freshUpdate = true;
                }

                map.LastActivityOn = tmxTrack.ActivityAt.UtcDateTime;

                await _repo.SaveAsync();

                await Task.Delay(500);

                var replays = await tmxTrack.GetReplaysAsync(tmx);

                if (replays.Results.Length == 0)
                {
                    continue;
                }

                var currentWr = map.WorldRecords
                    .Where(x => !x.Ignored)
                    .OrderByDescending(x => x.DrivenOn)
                    .FirstOrDefault();

                var isStunts = map.Mode?.Name == NameConsts.MapModeStunts;

                var wrsOnTmx = GetWrHistory(replays, isStunts).ToArray();

                var wrOnTmx = wrsOnTmx[^1];

                // If its a worse/equal time or score
                if (currentWr is not null)
                {
                    if (isStunts && wrOnTmx.ReplayScore <= currentWr.Time)
                    {
                        continue;
                    }

                    if (!isStunts && wrOnTmx.ReplayTime >= currentWr.Time)
                    {
                        continue;
                    }
                }

                var newWrsOnTmx = currentWr is null ? wrsOnTmx
                    : wrsOnTmx.Where(x => isStunts
                        ? x.ReplayScore > currentWr.Time
                        : x.ReplayTime < currentWr.Time);

                _logger.LogInformation("{count} new world record/s found.", newWrsOnTmx.Count());

                var previousWr = currentWr;

                foreach (var newWr in newWrsOnTmx)
                {
                    var timeOrScore = isStunts ? newWr.ReplayScore : newWr.ReplayTime;

                    // If this is the old world record time
                    if (currentWr is not null && (isStunts ? timeOrScore <= currentWr.Time : timeOrScore >= currentWr.Time))
                    {
                        _logger.LogInformation("Reached the end of new world records.");
                        break;
                    }

                    // If time is not legit
                    if (timeOrScore < 0 || (!isStunts && timeOrScore == 0))
                    {
                        _logger.LogInformation("Time is not legit!");
                        continue;
                    }

                    _logger.LogInformation("New world record!");

                    var difference = default(int?);

                    if (currentWr is not null)
                    {
                        difference = timeOrScore - currentWr.Time;
                    }

                    /*var wrModel = await AddNewWorldRecordAsync(db, tmxSite, map, newWr, previousWr,
                        time.ToMilliseconds(), cancellationToken);*/

                    var tmxLogin = await _repo.GetOrAddTmxLoginAsync(newWr.User.UserId, tmxSite);

                    tmxLogin.Nickname = newWr.User.Name;
                    tmxLogin.LastSeenOn = DateTime.UtcNow;

                    var wrModel = new WorldRecordModel
                    {
                        Guid = Guid.NewGuid(),
                        Map = map,
                        TmxPlayer = tmxLogin,
                        DrivenOn = newWr.ReplayAt.DateTime,
                        PublishedOn = newWr.ReplayAt.DateTime,
                        ReplayUrl = null,
                        Time = timeOrScore,
                        PreviousWorldRecord = previousWr,
                        ReplayId = newWr.ReplayId
                    };

                    await _repo.AddWorldRecordAsync(wrModel);

                    if (!freshUpdate)
                    {
                        var report = new ReportModel
                        {
                            Guid = Guid.NewGuid(),
                            Type = ReportModel.EType.NewWorldRecord,
                            HappenedOn = DateTime.UtcNow,
                            WorldRecord = wrModel
                        };

                        await _repo.AddReportAsync(report);

                        Discord.Embed embed;
                        if (map.Mode?.Name == NameConsts.MapModeStunts)
                            embed = _discordWebhookService.GetDefaultEmbed_NewStuntsWorldRecord(wrModel);
                        else
                            embed = _discordWebhookService.GetDefaultEmbed_NewWorldRecord(wrModel);

                        await SendMessageToAllWebhooksAsync(embed, report, leaderboardType);
                    }

                    await _repo.SaveAsync();

                    previousWr = wrModel;
                }
            }
        }
        while (!endOfNewActivities && recentTracks.HasMorePages);
    }

    private async Task SendMessageToAllWebhooksAsync(Discord.Embed embed, ReportModel report, LeaderboardType usedLeaderboardType)
    {
        foreach (var webhook in await _repo.GetDiscordWebhooksAsync())
        {
            if (string.IsNullOrWhiteSpace(webhook.Filter))
            {
                // Allows everything
            }
            else
            {
                try
                {
                    var filter = JsonHelper.Deserialize<DiscordWebhookFilter>(webhook.Filter);

                    if (filter.ReportTMUF is null)
                    {
                        continue;
                    }
                    else
                    {
                        var map = report.WorldRecord.Map;

                        if (map.TmxAuthor is null)
                        {
                            throw new Exception();
                        }

                        // If the filtered user+site do not match the map tmx author or leaderboard type
                        if (!filter.ReportTMUF.Any(x =>
                        {
                            if (x.Site != map.TmxAuthor.Site.ShortName)
                            {
                                return false;
                            }

                            if (x.UserId is not null && x.UserId == map.TmxAuthor.UserId)
                            {
                                return true;
                            }

                            if (x.LeaderboardType == usedLeaderboardType)
                            {
                                return true;
                            }

                            return false;
                        }))
                        {
                            continue;
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }

            await _discordWebhookService.SendMessageAsync(webhook, snowflake => new DiscordWebhookMessageModel
            {
                MessageId = snowflake,
                Report = report,
                SentOn = DateTime.UtcNow,
                ModifiedOn = DateTime.UtcNow,
                Webhook = webhook
            }, embeds: Enumerable.Repeat(embed, 1));
        }
    }

    private static IEnumerable<ReplayItem> GetWrHistory(ItemCollection<ReplayItem> replays, bool isStunts = false)
    {
        var tempTime = default(int?);

        foreach (var replay in replays.Results.OrderBy(x => x.ReplayAt))
        {
            if (tempTime is null)
            {
                tempTime = isStunts ? replay.ReplayScore : replay.ReplayTime;
                yield return replay;
            }

            if (isStunts && replay.ReplayScore > tempTime)
            {
                tempTime = replay.ReplayScore;
                yield return replay;
            }

            if (!isStunts && replay.ReplayTime < tempTime)
            {
                tempTime = replay.ReplayTime;
                yield return replay;
            }
        }
    }
}
