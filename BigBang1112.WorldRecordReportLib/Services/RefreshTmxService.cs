using BigBang1112.Services;
using BigBang1112.WorldRecordReportLib.Data;
using BigBang1112.WorldRecordReportLib.Enums;
using BigBang1112.WorldRecordReportLib.Models;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Models.ReportScopes;
using ManiaAPI.TMX;
using Mapster;
using Microsoft.Extensions.Logging;

using TmxSite = BigBang1112.WorldRecordReportLib.Enums.TmxSite;

namespace BigBang1112.WorldRecordReportLib.Services;

public class RefreshTmxService : RefreshService
{
    private const string ScopeOfficialWR = $"{nameof(ReportScopeSet.TMUF)}:{nameof(ReportScopeTMUF.TMX)}:{nameof(ReportScopeTmx.Official)}:{nameof(ReportScopeTmxOfficial.WR)}";
    private const string ScopeOfficialChanges = $"{nameof(ReportScopeSet.TMUF)}:{nameof(ReportScopeTMUF.TMX)}:{nameof(ReportScopeTmx.Official)}:{nameof(ReportScopeTmxOfficial.Changes)}";

    private readonly ITmxService _tmxService;
    private readonly IWrUnitOfWork _wrUnitOfWork;
    private readonly RecordStorageService _recordStorageService;
    private readonly IDiscordWebhookService _discordWebhookService;
    private readonly ReportService _reportService;
    private readonly ILogger<RefreshTmxService> _logger;

    public RefreshTmxService(ITmxService tmxService,
                            IWrUnitOfWork wrUnitOfWork,
                            RecordStorageService recordStorageService,
                            IDiscordWebhookService discordWebhookService,
                            ReportService reportService,
                            ILogger<RefreshTmxService> logger) : base(logger)
    {
        _tmxService = tmxService;
        _wrUnitOfWork = wrUnitOfWork;
        _recordStorageService = recordStorageService;
        _discordWebhookService = discordWebhookService;
        _reportService = reportService;
        _logger = logger;
    }

    public async Task CleanupRemovedWorldRecordsAsync()
    {
        var lastTmxWrs = await _wrUnitOfWork.WorldRecords.GetLatestByGameAsync(Game.TMUF, count: 5);
        await CleanupWorldRecordsAsync(lastTmxWrs, false);
    }

    public async Task UpdateWorldRecordsAsync(TmxSite tmxSite, LeaderboardType leaderboardType, string subScope)
    {
        var maniaApiTmxSite = TmxSiteToManiaApiTmxSite(tmxSite);

        var endOfNewActivities = false;

        var recentTracks = default(ItemCollection<TrackSearchItem>);

        do
        {
            _logger.LogInformation("Searching Nadeo United maps, most recent activity...");

            recentTracks = await _tmxService.SearchAsync(maniaApiTmxSite, new TrackSearchFilters
            {
                PrimaryOrder = TrackOrder.ActivityMostRecent,
                LeaderboardType = leaderboardType,
                AfterTrackId = recentTracks?.Results.LastOrDefault()?.TrackId
            });

            _logger.LogInformation("{count} maps found.", recentTracks.Results.Length);

            foreach (var tmxTrack in recentTracks.Results)
            {
                endOfNewActivities = await CheckTrackForNewRecordsAsync(tmxSite, tmxTrack, subScope);

                if (endOfNewActivities)
                {
                    break;
                }
            }
        }
        while (!endOfNewActivities && recentTracks.HasMorePages);
    }

    private static ManiaAPI.TMX.TmxSite TmxSiteToManiaApiTmxSite(TmxSite site) => site switch
    {
        TmxSite.United => ManiaAPI.TMX.TmxSite.United,
        TmxSite.TMNF => ManiaAPI.TMX.TmxSite.TMNForever,
        _ => throw new NotImplementedException(),
    };

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tmxSite"></param>
    /// <param name="tmxTrack"></param>
    /// <param name="leaderboardType"></param>
    /// <returns>True if the end of activities was reached.</returns>
    private async Task<bool> CheckTrackForNewRecordsAsync(TmxSite tmxSite, TrackSearchItem tmxTrack, string subScope)
    {
        var maniaApiTmxSite = TmxSiteToManiaApiTmxSite(tmxSite);
        
        var map = await _wrUnitOfWork.Maps.GetByMxIdAsync(tmxTrack.TrackId, tmxSite);

        if (map is null)
        {
            return false;
        }

        if (!_recordStorageService.TmxLeaderboardExists(tmxSite, map.MapUid))
        {
            var firstRecordSet = await _tmxService.GetReplaysAsync(maniaApiTmxSite, tmxTrack.TrackId);
            
            if (firstRecordSet.Results.Length == 0)
            {
                return false;
            }

            await _recordStorageService.SaveTmxLeaderboardAsync(firstRecordSet.Results.Adapt<TmxReplay[]>(), tmxSite, map.MapUid);

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

        map.LastActivityOn = tmxTrack.ActivityAt;

        await _wrUnitOfWork.SaveAsync();

        await Task.Delay(500);

        var replays = await _tmxService.GetReplaysAsync(maniaApiTmxSite, tmxTrack.TrackId);

        if (replays.Results.Length == 0)
        {
            return false;
        }

        var recordSet = replays.Results.Adapt<TmxReplay[]>();
        var prevRecordSet = await _recordStorageService.GetTmxLeaderboardAsync(tmxSite, map.MapUid);

        if (prevRecordSet is not null && !map.IsStuntsMode()) // TODO: stunts changes
        {
            // check for leaderboard changes
            // ...
            await ReportChangesInRecordsAsync(map, recordSet, prevRecordSet, subScope);
        }

        // create a .json.gz file from replays.Results
        await _recordStorageService.SaveTmxLeaderboardAsync(recordSet, tmxSite, map.MapUid);

        var currentWr = map.WorldRecords
            .Where(x => x.Ignored == IgnoredMode.NotIgnored)
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

            if (!isStunts && wrOnTmx.ReplayTime.TotalMilliseconds >= currentWr.Time)
            {
                return false;
            }
        }

        var newWrsOnTmx = currentWr is null ? wrsOnTmx
            : wrsOnTmx.Where(x => isStunts
                ? x.ReplayScore > currentWr.Time
                : x.ReplayTime.TotalMilliseconds < currentWr.Time);

        _logger.LogInformation("{count} new world record/s found.", newWrsOnTmx.Count());

        var previousWr = currentWr;

        foreach (var newWr in newWrsOnTmx)
        {
            var newTimeOrScore = isStunts ? newWr.ReplayScore : newWr.ReplayTime.TotalMilliseconds;

            // If this is the old world record time
            if (previousWr is not null && (isStunts ? newTimeOrScore <= previousWr.Time : newTimeOrScore >= previousWr.Time))
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
                previousWr,
                newTimeOrScore,
                newWr.User,
                newWr.ReplayAt,
                newWr.ReplayId,
                freshUpdate,
                subScope);

            previousWr = wrModel;
        }

        return false;
    }

    private async ValueTask ReportChangesInRecordsAsync(MapModel map, IEnumerable<TmxReplay> records, IEnumerable<TmxReplay> prevRecords, string subScope)
    {
        var changes = LeaderboardComparer.Compare(records.Where(x => x.Rank.HasValue), prevRecords.Where(x => x.Rank.HasValue));

        if (changes is null)
        {
            return;
        }

        var rich = CreateLeaderboardChangesRich(changes,
            records.Where(x => x.Rank.HasValue).ToDictionary(x => x.UserId, x => x as IRecord<int>),
            prevRecords.Where(x => x.Rank.HasValue).ToDictionary(x => x.UserId, x => x as IRecord<int>));

        if (rich is null)
        {
            return;
        }

        await _reportService.ReportDifferencesAsync(rich, map, $"{ScopeOfficialChanges}:{subScope}", maxRank: 10);
    }

    private async Task<WorldRecordModel> ProcessNewWorldRecordAsync(TmxSite tmxSite,
                                                                    MapModel map,
                                                                    WorldRecordModel? currentWr,
                                                                    int newTimeOrScore,
                                                                    User user,
                                                                    DateTime replayAt,
                                                                    int replayId,
                                                                    bool freshUpdate,
                                                                    string subScope)
    {
        _logger.LogInformation("New world record!");

        var difference = default(int?);

        if (currentWr is not null)
        {
            difference = newTimeOrScore - currentWr.Time;
        }

        /*var wrModel = await AddNewWorldRecordAsync(db, tmxSite, map, newWr, previousWr,
            time.ToMilliseconds(), cancellationToken);*/

        var tmxLogin = await _wrUnitOfWork.TmxLogins.GetOrAddAsync(user.UserId, tmxSite);

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

        await _wrUnitOfWork.WorldRecords.AddAsync(wrModel);

        if (!freshUpdate)
        {
            await _reportService.ReportWorldRecordAsync(wrModel, $"{ScopeOfficialWR}:{subScope}");
        }

        await _wrUnitOfWork.SaveAsync();

        return wrModel;
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

            if (map.MxId is null)
            {
                throw new Exception();
            }

            var tmxId = map.MxId.Value;
            var siteEnum = site.GetSiteEnum();

            var replays = await _tmxService.GetReplaysAsync(siteEnum, tmxId);

            if (map.Mode?.Id == (int)MapMode.Stunts)
            {
                // TODO
                continue;
            }

            var tmxWrs = _tmxService.GetWrHistory(replays).Reverse();
            var tmxWr = tmxWrs.FirstOrDefault();

            if (tmxWr is not null && tmxWr.ReplayTime.TotalMilliseconds <= wr.Time)
            {
                continue;
            }

            // The record was removed and there are no more records
            // or the TMX record is slower than WR in the database

            wr.Ignored = IgnoredMode.Ignored;

            var report = await _wrUnitOfWork.Reports.GetByWorldRecordAsync(wr);

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
                await _wrUnitOfWork.SaveAsync();

                var otherWrsByThisUser = await _wrUnitOfWork.WorldRecords.GetByTmxPlayerAsync(wr.TmxPlayer);

                if (otherWrsByThisUser.Any())
                {
                    await CleanupWorldRecordsAsync(otherWrsByThisUser, true);
                }
            }

            await _wrUnitOfWork.SaveAsync();
        }
    }
}
