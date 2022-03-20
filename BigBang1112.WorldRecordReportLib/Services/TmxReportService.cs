using BigBang1112.Services;
using BigBang1112.WorldRecordReportLib.Data;
using BigBang1112.WorldRecordReportLib.Models;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;
using Mapster;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Text.Json;
using TmExchangeApi;
using TmExchangeApi.Models;

namespace BigBang1112.WorldRecordReportLib.Services;

public class TmxReportService
{
    private readonly ITmxService _tmxService;
    private readonly IWrRepo _repo;
    private readonly IDiscordWebhookService _discordWebhookService;
    private readonly IFileHostService _fileHostService;
    private readonly ITmxRecordSetService _tmxRecordSetService;
    private readonly ILogger<TmxReportService> _logger;

    public TmxReportService(ITmxService tmxService,
                            IWrRepo repo,
                            IDiscordWebhookService discordWebhookService,
                            IFileHostService fileHostService,
                            ITmxRecordSetService tmxRecordSetService,
                            ILogger<TmxReportService> logger)
    {
        _tmxService = tmxService;
        _repo = repo;
        _discordWebhookService = discordWebhookService;
        _fileHostService = fileHostService;
        _tmxRecordSetService = tmxRecordSetService;
        _logger = logger;
    }

    public async Task CleanupRemovedWorldRecordsAsync()
    {
        var lastTmxWrs = await _repo.GetLastWorldRecordsInTMUFAsync(5);
        await CleanupWorldRecordsAsync(lastTmxWrs, false);
    }

    public async Task UpdateWorldRecordsAsync(TmxSite site, LeaderboardType leaderboardType)
    {
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

            recentTracks = await _tmxService.SearchAsync(site, new TrackSearchFilters
            {
                PrimaryOrder = TrackOrder.ActivityMostRecent,
                LeaderboardType = leaderboardType,
                AfterTrackId = recentTracks?.Results.LastOrDefault()?.TrackId
            });

            _logger.LogInformation("{count} maps found.", recentTracks.Results.Length);

            foreach (var tmxTrack in recentTracks.Results)
            {
                endOfNewActivities = await CheckTrackForNewRecordsAsync(tmxSite, tmxTrack, leaderboardType);

                if (endOfNewActivities)
                {
                    break;
                }
            }
        }
        while (!endOfNewActivities && recentTracks.HasMorePages);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tmxSite"></param>
    /// <param name="tmxTrack"></param>
    /// <param name="leaderboardType"></param>
    /// <returns>True if the end of activities was reached.</returns>
    private async Task<bool> CheckTrackForNewRecordsAsync(TmxSiteModel tmxSite, TrackSearchItem tmxTrack, LeaderboardType leaderboardType)
    {
        var map = await _repo.GetMapByMxIdAsync(tmxTrack.TrackId, tmxSite);

        if (map is null)
        {
            return false;
        }

        if (!_tmxRecordSetService.RecordSetExists(tmxSite, map))
        {
            var firstRecordSet = await _tmxService.GetReplaysAsync(tmxSite, tmxTrack.TrackId);
            
            if (firstRecordSet.Results.Length == 0)
            {
                return false;
            }

            await _tmxRecordSetService.SaveRecordSetAsync(tmxSite, map, firstRecordSet);

            await Task.Delay(500); // Give some breathing to TMX API

            // return false indicating that further files should get checked for creation
            return false;
        }

        if (map.LastActivityOn is not null && tmxTrack.ActivityAt.Ticks - (tmxTrack.ActivityAt.Ticks % TimeSpan.TicksPerSecond) <= map.LastActivityOn.Value.Ticks)
        {
            return true;
        }

        var freshUpdate = false;

        if (map.LastActivityOn is null)
        {
            _logger.LogInformation("Fresh activity, world records are not going to be reported. {mapname}", map.DeformattedName);
            freshUpdate = true;
        }

        map.LastActivityOn = tmxTrack.ActivityAt.DateTime;

        await _repo.SaveAsync();

        await Task.Delay(500);

        var replays = await _tmxService.GetReplaysAsync(tmxSite, tmxTrack.TrackId);

        if (replays.Results.Length == 0)
        {
            return false;
        }

        var recordSet = replays.Results.Adapt<TmxReplay[]>();
        var prevRecordSet = await _tmxRecordSetService.GetRecordSetAsync(tmxSite, map);

        // check for leaderboard changes
        // ...
        FindChangesInRecordSets(prevRecordSet, recordSet);

        // create a .json.gz file from replays.Results
        await _tmxRecordSetService.SaveRecordSetAsync(tmxSite, map, recordSet);

        var currentWr = map.WorldRecords
            .Where(x => !x.Ignored)
            .OrderByDescending(x => x.DrivenOn)
            .FirstOrDefault();

        var isStunts = map.IsStuntsMode();

        var wrsOnTmx = _tmxService.GetWrHistory(replays, isStunts).ToArray();

        var wrOnTmx = wrsOnTmx[^1];

        // If its a worse/equal time or score
        if (currentWr is not null)
        {
            if (isStunts && wrOnTmx.ReplayScore <= currentWr.Time)
            {
                return false;
            }

            if (!isStunts && wrOnTmx.ReplayTime >= currentWr.Time)
            {
                return false;
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
            var newTimeOrScore = isStunts ? newWr.ReplayScore : newWr.ReplayTime;

            // If this is the old world record time
            if (currentWr is not null && (isStunts ? newTimeOrScore <= currentWr.Time : newTimeOrScore >= currentWr.Time))
            {
                _logger.LogInformation("Reached the end of new world records.");
                break;
            }

            // If time is not legit
            if (newTimeOrScore < 0 || (!isStunts && newTimeOrScore == 0))
            {
                _logger.LogWarning("Time is not legit!");
                continue;
            }

            var wrModel = await ProcessNewWorldRecordAsync(
                tmxSite,
                map,
                currentWr,
                newTimeOrScore,
                newWr.User,
                newWr.ReplayAt.DateTime,
                newWr.ReplayId,
                freshUpdate,
                leaderboardType);

            previousWr = wrModel;
        }

        return false;
    }

    private void FindChangesInRecordSets(TmxReplay[] prevRecordSet, TmxReplay[] recordSet)
    {
        
    }

    private async Task<WorldRecordModel> ProcessNewWorldRecordAsync(TmxSiteModel tmxSite,
                                                                    MapModel map,
                                                                    WorldRecordModel? currentWr,
                                                                    int newTimeOrScore,
                                                                    User user,
                                                                    DateTime replayAt,
                                                                    int replayId,
                                                                    bool freshUpdate,
                                                                    LeaderboardType leaderboardType)
    {
        _logger.LogInformation("New world record!");

        var difference = default(int?);

        if (currentWr is not null)
        {
            difference = newTimeOrScore - currentWr.Time;
        }

        /*var wrModel = await AddNewWorldRecordAsync(db, tmxSite, map, newWr, previousWr,
            time.ToMilliseconds(), cancellationToken);*/

        var tmxLogin = await _repo.GetOrAddTmxLoginAsync(user.UserId, tmxSite);

        tmxLogin.Nickname = user.Name;
        tmxLogin.LastSeenOn = DateTime.UtcNow;

        var wrModel = new WorldRecordModel
        {
            Guid = Guid.NewGuid(),
            Map = map,
            TmxPlayer = tmxLogin,
            DrivenOn = replayAt,
            PublishedOn = replayAt,
            ReplayUrl = null,
            Time = newTimeOrScore,
            PreviousWorldRecord = currentWr,
            ReplayId = replayId
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

        return wrModel;
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

    internal async Task CleanupWorldRecordsAsync(IEnumerable<WorldRecordModel> lastTmxWrs, bool onlyOneUser)
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
            {
                throw new Exception();
            }

            var tmxId = map.MxId.Value;
            var siteEnum = site.GetSiteEnum();

            var replays = await _tmxService.GetReplaysAsync(siteEnum, tmxId);

            if (map.Mode?.Name == NameConsts.MapModeStunts)
            {
                // TODO
                continue;
            }

            var tmxWrs = _tmxService.GetWrHistory(replays).Reverse();
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
            {
                throw new Exception();
            }

            foreach (var msg in report.DiscordWebhookMessages)
            {
                await _discordWebhookService.DeleteMessageAsync(msg);
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
