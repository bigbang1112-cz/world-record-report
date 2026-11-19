using BigBang1112.Services;
using BigBang1112.WorldRecordReportLib.Data;
using BigBang1112.WorldRecordReportLib.Enums;
using BigBang1112.WorldRecordReportLib.Models;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Models.ReportScopes;
using ManiaAPI.TMX;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using TmxSite = BigBang1112.WorldRecordReportLib.Enums.TmxSite;

namespace BigBang1112.WorldRecordReportLib.Services;

public class RefreshTmxService : RefreshService
{
    private const string ScopeOfficialWR = $"{nameof(ReportScopeSet.TMUF)}:{nameof(ReportScopeTMUF.TMX)}:{nameof(ReportScopeTmx.Official)}:{nameof(ReportScopeTmxOfficial.WR)}";
    private const string ScopeOfficialChanges = $"{nameof(ReportScopeSet.TMUF)}:{nameof(ReportScopeTMUF.TMX)}:{nameof(ReportScopeTmx.Official)}:{nameof(ReportScopeTmxOfficial.Changes)}";
    private const string ScopeTMNESWCOfficialWR = $"{nameof(ReportScopeSet.TMNESWC)}:{nameof(ReportScopeTMNESWC.TMX)}:{nameof(ReportScopeTmxTMNESWC.Official)}:{nameof(ReportScopeTmxTMNESWCOfficial.WR)}";
    private const string ScopeTMNESWCOfficialChanges = $"{nameof(ReportScopeSet.TMNESWC)}:{nameof(ReportScopeTMNESWC.TMX)}:{nameof(ReportScopeTmxTMNESWC.Official)}:{nameof(ReportScopeTmxTMNESWCOfficial.Changes)}";

    private readonly ITmxService _tmxService;
    private readonly IWrUnitOfWork _wrUnitOfWork;
    private readonly RecordStorageService _recordStorageService;
    private readonly ReportService _reportService;
    private readonly TmxFailedTrackQueue _failedTracks;
    private readonly ILogger<RefreshTmxService> _logger;

    public RefreshTmxService(ITmxService tmxService,
                             IWrUnitOfWork wrUnitOfWork,
                             RecordStorageService recordStorageService,
                             ReportService reportService,
                             TmxFailedTrackQueue failedTracks,
                             ILogger<RefreshTmxService> logger) : base(logger)
    {
        _tmxService = tmxService;
        _wrUnitOfWork = wrUnitOfWork;
        _recordStorageService = recordStorageService;
        _reportService = reportService;
        _failedTracks = failedTracks;
        _logger = logger;
    }

    public async Task CleanupRemovedWorldRecordsAsync()
    {
        var lastTmxWrs = await _wrUnitOfWork.WorldRecords.GetLatestByGameAsync(Game.TMUF, count: 25);
        await CleanupWorldRecordsAsync(lastTmxWrs, false);

        var lastTmxTMNESWCWrs = await _wrUnitOfWork.WorldRecords.GetLatestByGameAsync(Game.TMN, count: 25);
        await CleanupWorldRecordsAsync(lastTmxTMNESWCWrs, false);
    }

    public async Task UpdateWorldRecordsAsync(TmxSite tmxSite, LeaderboardType leaderboardType, string subScope)
    {
        var maniaApiTmxSite = TmxSiteToManiaApiTmxSite(tmxSite);

        var failedAgain = await RetryFailedTracksAsync(tmxSite, subScope);

        var endOfNewActivities = false;

        var recentTracks = default(ItemCollection<TrackItem>);

        do
        {
            _logger.LogInformation("Searching Nadeo United maps, most recent activity...");

            recentTracks = await _tmxService.SearchAsync(maniaApiTmxSite, new TMX.SearchTracksParameters
            {
                Order1 = TrackOrder.ActivityMostRecent,
                LbType = leaderboardType,
                After = recentTracks?.Results.LastOrDefault()?.TrackId
            });

            _logger.LogInformation("{count} maps found.", recentTracks.Results.Count);

            foreach (var tmxTrack in recentTracks.Results)
            {
                // Its retry already failed in this run, don't hit it twice
                if (failedAgain.Contains(tmxTrack.TrackId))
                {
                    continue;
                }

                try
                {
                    endOfNewActivities = await CheckTrackForNewRecordsAsync(tmxSite, tmxTrack, subScope);
                    _failedTracks.Remove(tmxSite, subScope, tmxTrack.TrackId);
                }
                catch (Exception ex)
                {
                    RegisterTrackFailure(ex, tmxSite, subScope, tmxTrack);

                    // DB context may be left in a broken state, end the run
                    if (ex is DbUpdateException)
                    {
                        throw;
                    }

                    // A single broken track should not block the remaining ones
                    continue;
                }

                if (endOfNewActivities)
                {
                    break;
                }
            }
        }
        while (!endOfNewActivities && recentTracks.HasMoreItems);
    }

    /// <summary>
    /// Retries tracks that failed in previous runs. The scan below stops at the first already-handled track,
    /// so it would otherwise never reach them again.
    /// </summary>
    /// <returns>IDs of tracks that failed again.</returns>
    private async Task<HashSet<long>> RetryFailedTracksAsync(TmxSite tmxSite, string subScope)
    {
        var failedAgain = new HashSet<long>();

        foreach (var failed in _failedTracks.GetAll(tmxSite, subScope))
        {
            var tmxTrack = failed.Track;

            _logger.LogInformation("Retrying TMX track {trackId} (attempt {attempt}/{max})...",
                tmxTrack.TrackId, failed.Attempts + 1, TmxFailedTrackQueue.MaxAttempts);

            try
            {
                await CheckTrackForNewRecordsAsync(tmxSite, tmxTrack, subScope);
                _failedTracks.Remove(tmxSite, subScope, tmxTrack.TrackId);
            }
            catch (Exception ex)
            {
                failedAgain.Add(tmxTrack.TrackId);

                RegisterTrackFailure(ex, tmxSite, subScope, tmxTrack);

                if (ex is DbUpdateException)
                {
                    throw;
                }
            }
        }

        return failedAgain;
    }

    private void RegisterTrackFailure(Exception ex, TmxSite tmxSite, string subScope, TrackItem tmxTrack)
    {
        var attempts = _failedTracks.RegisterFailure(tmxSite, subScope, tmxTrack);

        if (attempts >= TmxFailedTrackQueue.MaxAttempts)
        {
            _logger.LogError(ex, "Failed to check TMX track {trackId} for new records ({attempts}/{max}), giving up. Its current activity will not be checked again.",
                tmxTrack.TrackId, attempts, TmxFailedTrackQueue.MaxAttempts);
        }
        else
        {
            _logger.LogError(ex, "Failed to check TMX track {trackId} for new records ({attempts}/{max}), retrying next run.",
                tmxTrack.TrackId, attempts, TmxFailedTrackQueue.MaxAttempts);
        }
    }

    private static ManiaAPI.TMX.TmxSite TmxSiteToManiaApiTmxSite(TmxSite site) => site switch
    {
        TmxSite.United => ManiaAPI.TMX.TmxSite.TMUF,
        TmxSite.TMNF => ManiaAPI.TMX.TmxSite.TMNF,
        TmxSite.Nations => ManiaAPI.TMX.TmxSite.Nations,
        _ => throw new NotImplementedException(),
    };

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tmxSite"></param>
    /// <param name="tmxTrack"></param>
    /// <param name="leaderboardType"></param>
    /// <returns>True if the end of activities was reached.</returns>
    private async Task<bool> CheckTrackForNewRecordsAsync(TmxSite tmxSite, TrackItem tmxTrack, string subScope)
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
            
            if (firstRecordSet.Results.Count == 0)
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

        await ProcessTrackActivityAsync(tmxSite, tmxTrack, map, subScope);

        // Mark the activity as handled only once everything above succeeded,
        // otherwise a failure would make the next run skip this activity for good
        map.LastActivityOn = tmxTrack.ActivityAt.UtcDateTime;

        await _wrUnitOfWork.SaveAsync();

        return false;
    }

    private async Task ProcessTrackActivityAsync(TmxSite tmxSite, TrackItem tmxTrack, MapModel map, string subScope)
    {
        var maniaApiTmxSite = TmxSiteToManiaApiTmxSite(tmxSite);

        var freshUpdate = false;

        /*if (map.LastActivityOn is null)
        {
            _logger.LogInformation("Fresh activity, world records are not going to be reported. {mapname}", map.DeformattedName);
            freshUpdate = true;
        }*/

        await Task.Delay(500);

        var replays = await _tmxService.GetReplaysAsync(maniaApiTmxSite, tmxTrack.TrackId);

        if (replays.Results.Count == 0)
        {
            return;
        }

        var recordSet = replays.Results.Adapt<TmxReplay[]>();
        var prevRecordSet = await _recordStorageService.GetTmxLeaderboardAsync(tmxSite, map.MapUid);

        if (prevRecordSet is not null && !map.IsStuntsMode()) // TODO: stunts changes
        {
            // check for leaderboard changes
            // ...
            await ReportChangesInRecordsAsync(map, recordSet, prevRecordSet, subScope, tmxSite);
        }

        // create a .json.gz file from replays.Results
        await _recordStorageService.SaveTmxLeaderboardAsync(recordSet, tmxSite, map.MapUid);

        var currentWr = map.WorldRecords
            .Where(x => x.Ignored == IgnoredMode.NotIgnored)
            .OrderByDescending(x => x.DrivenOn)
            .FirstOrDefault();

        var isStunts = map.IsStuntsMode();

        // Unranked replays were driven on an older version of the track, they must not count as world records
        var wrsOnTmx = _tmxService.GetWrHistory(tmxTrack.UpdatedAt, replays, isStunts, rankedOnly: true).ToArray();

        if (wrsOnTmx.Length == 0)
        {
            return;
        }

        var wrOnTmx = wrsOnTmx[^1];

        // If its a worse/equal time or score
        if (currentWr is not null)
        {
            if (isStunts && wrOnTmx.ReplayScore <= currentWr.Time)
            {
                return;
            }

            if (!isStunts && wrOnTmx.ReplayTime.TotalMilliseconds >= currentWr.Time)
            {
                return;
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
                newWr.ReplayAt.UtcDateTime,
                newWr.ReplayId,
                freshUpdate,
                subScope);

            previousWr = wrModel;
        }
    }

    private async ValueTask ReportChangesInRecordsAsync(MapModel map, IEnumerable<TmxReplay> records, IEnumerable<TmxReplay> prevRecords, string subScope, TmxSite tmxSite)
    {
        var changes = LeaderboardComparer.Compare(records.Where(x => x.Rank.HasValue), prevRecords.Where(x => x.Rank.HasValue));

        if (changes is null)
        {
            return;
        }

        var currentRecords = new Dictionary<int, IRecord<int>>();
        foreach (var rec in records.Where(x => x.Rank.HasValue))
            currentRecords[rec.UserId] = rec;

        var previousRecords = new Dictionary<int, IRecord<int>>();
        foreach (var rec in prevRecords.Where(x => x.Rank.HasValue))
            previousRecords[rec.UserId] = rec;

        var rich = CreateLeaderboardChangesRich(changes, currentRecords, previousRecords);

        if (rich is null)
        {
            return;
        }

        await _reportService.ReportDifferencesAsync(rich, map, $"{(tmxSite == TmxSite.Nations ? ScopeTMNESWCOfficialChanges : ScopeOfficialChanges)}:{subScope}", maxRank: 10);
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

        if (!freshUpdate && DateTime.UtcNow - replayAt < TimeSpan.FromDays(14))
        {
            await _reportService.ReportWorldRecordAsync(wrModel, $"{(tmxSite == TmxSite.Nations ? ScopeTMNESWCOfficialWR : ScopeOfficialWR)}:{subScope}");
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

            var tmxWrs = _tmxService.GetWrHistory(map.UpdatedOn, replays).Reverse();
            var tmxWr = tmxWrs.FirstOrDefault();

            if (tmxWr is not null && tmxWr.ReplayTime.TotalMilliseconds <= wr.Time)
            {
                continue;
            }

            // The record was removed and there are no more records
            // or the TMX record is slower than WR in the database

            wr.Ignored = IgnoredMode.Ignored;

            // Deletes both webhook and bot messages; does nothing for unreported WRs (e.g. older than 14 days)
            await _reportService.RemoveWorldRecordReportAsync(wr);

            await _wrUnitOfWork.SaveAsync();

            if (!onlyOneUser)
            {
                var otherWrsByThisUser = await _wrUnitOfWork.WorldRecords.GetByTmxPlayerAsync(wr.TmxPlayer);

                if (otherWrsByThisUser.Any())
                {
                    await CleanupWorldRecordsAsync(otherWrsByThisUser, true);
                }
            }
        }
    }
}
