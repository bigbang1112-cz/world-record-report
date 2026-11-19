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
    private readonly TmxFullScanRequest _fullScanRequest;
    private readonly ILogger<RefreshTmxService> _logger;

    public RefreshTmxService(ITmxService tmxService,
                             IWrUnitOfWork wrUnitOfWork,
                             RecordStorageService recordStorageService,
                             ReportService reportService,
                             TmxFailedTrackQueue failedTracks,
                             TmxFullScanRequest fullScanRequest,
                             ILogger<RefreshTmxService> logger) : base(logger)
    {
        _tmxService = tmxService;
        _wrUnitOfWork = wrUnitOfWork;
        _recordStorageService = recordStorageService;
        _reportService = reportService;
        _failedTracks = failedTracks;
        _fullScanRequest = fullScanRequest;
        _logger = logger;
    }

    public async Task CleanupRemovedWorldRecordsAsync()
    {
        // The cleanup only detects removals. The full scan they trigger handles them on all tracks,
        // including announcing the world record that replaces the removed one
        var lastTmxWrs = await _wrUnitOfWork.WorldRecords.GetLatestByGameAsync(Game.TMUF, count: 25);

        if (await RequestFullScanIfRemovedAsync(lastTmxWrs))
        {
            return;
        }

        var lastTmxTMNESWCWrs = await _wrUnitOfWork.WorldRecords.GetLatestByGameAsync(Game.TMN, count: 25);

        await RequestFullScanIfRemovedAsync(lastTmxTMNESWCWrs);
    }

    /// <param name="fullScan">Check all tracks instead of stopping at the first one with no new activity.</param>
    public async Task UpdateWorldRecordsAsync(TmxSite tmxSite, LeaderboardType leaderboardType, string subScope, bool fullScan = false)
    {
        var maniaApiTmxSite = TmxSiteToManiaApiTmxSite(tmxSite);

        if (fullScan)
        {
            _logger.LogInformation("Full scan of {site} {leaderboardType} tracks...", tmxSite, leaderboardType);
        }

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

            // Without results there is no cursor for the next page, it would start over from the first one
            if (recentTracks.Results.Count == 0)
            {
                break;
            }

            foreach (var tmxTrack in recentTracks.Results)
            {
                // Its retry already failed in this run, don't hit it twice
                if (failedAgain.Contains(tmxTrack.TrackId))
                {
                    continue;
                }

                try
                {
                    var endReached = await CheckTrackForNewRecordsAsync(tmxSite, tmxTrack, subScope, fullScan);

                    // A full scan goes through all tracks
                    endOfNewActivities = endReached && !fullScan;

                    _failedTracks.Remove(tmxSite, subScope, tmxTrack.TrackId);
                }
                catch (Exception ex)
                {
                    HandleTrackFailure(ex, tmxSite, subScope, tmxTrack);

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
                await CheckTrackForNewRecordsAsync(tmxSite, tmxTrack, subScope, fullScan: false);
                _failedTracks.Remove(tmxSite, subScope, tmxTrack.TrackId);
            }
            catch (Exception ex)
            {
                failedAgain.Add(tmxTrack.TrackId);

                HandleTrackFailure(ex, tmxSite, subScope, tmxTrack);

                if (ex is DbUpdateException)
                {
                    throw;
                }
            }
        }

        return failedAgain;
    }

    private void HandleTrackFailure(Exception ex, TmxSite tmxSite, string subScope, TrackItem tmxTrack)
    {
        // The failed track may have left changes pending (e.g. an ignored world record, a new world record or its report).
        // They must not be saved by the next track, otherwise the retry would see the track as already processed.
        _wrUnitOfWork.DiscardChanges();

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
    /// <param name="fullScan">Process the track even without new activity.</param>
    /// <returns>True if the end of activities was reached.</returns>
    private async Task<bool> CheckTrackForNewRecordsAsync(TmxSite tmxSite, TrackItem tmxTrack, string subScope, bool fullScan)
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

        var activityHandled = map.LastActivityOn is not null
            && tmxTrack.ActivityAt.Ticks - (tmxTrack.ActivityAt.Ticks % TimeSpan.TicksPerSecond) <= map.LastActivityOn.Value.Ticks;

        if (activityHandled && !fullScan)
        {
            return true;
        }

        var removalsFound = await ProcessTrackActivityAsync(tmxSite, tmxTrack, map, subScope, fullScan);

        // Mark the activity as handled only once everything above succeeded,
        // otherwise a failure would make the next run skip this activity for good
        map.LastActivityOn = tmxTrack.ActivityAt.UtcDateTime;

        await _wrUnitOfWork.SaveAsync();

        if (removalsFound && !fullScan)
        {
            // The removal (typically of a cheater's replays) can affect other tracks without any new activity on them
            _logger.LogInformation("Removed world record found during a regular refresh, requesting a full TMX scan.");
            _fullScanRequest.Request();
        }

        return false;
    }

    /// <returns>True if removed world records were found.</returns>
    private async Task<bool> ProcessTrackActivityAsync(TmxSite tmxSite, TrackItem tmxTrack, MapModel map, string subScope, bool fullScan)
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
            return false;
        }

        var recordSet = replays.Results.Adapt<TmxReplay[]>();
        var prevRecordSet = await _recordStorageService.GetTmxLeaderboardAsync(tmxSite, map.MapUid);

        if (prevRecordSet is not null && !map.IsStuntsMode()) // TODO: stunts changes
        {
            await ReportChangesInRecordsAsync(map, recordSet, prevRecordSet, subScope, tmxSite, fullScan);
        }

        // create a .json.gz file from replays.Results
        await _recordStorageService.SaveTmxLeaderboardAsync(recordSet, tmxSite, map.MapUid);

        var currentWr = GetCurrentWorldRecord(map);

        var isStunts = map.IsStuntsMode();

        // Unranked replays were driven on an older version of the track, they must not count as world records
        var wrsOnTmx = _tmxService.GetWrHistory(tmxTrack.UpdatedAt, replays, isStunts, rankedOnly: true).ToArray();

        // A stored WR without an equal or better replay on TMX was removed, same as in TM2
        var removedWrs = isStunts // TODO: stunts removals
            ? []
            : IgnoreRemovedWorldRecords(map, currentWr, tmxTrack, replays);

        if (removedWrs.Count > 0)
        {
            await ProcessRemovedWorldRecordsAsync(tmxSite, map, removedWrs, wrsOnTmx.LastOrDefault(), subScope);
            return true;
        }

        if (wrsOnTmx.Length == 0)
        {
            return false;
        }

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
                newWr.ReplayAt.UtcDateTime,
                newWr.ReplayId,
                freshUpdate,
                subScope);

            previousWr = wrModel;
        }

        return false;
    }

    /// <summary>
    /// Same as TM2: if the current TMX world record is a stored one that survived the removal, the reports of the removed ones
    /// are deleted. Otherwise the TMX world record is added and announced in a single "Removed world record" report.
    /// </summary>
    /// <param name="removedWrs">Already ignored, the most recent one first.</param>
    /// <param name="wrOnTmx">Best ranked replay on TMX, null if there is none.</param>
    private async Task ProcessRemovedWorldRecordsAsync(TmxSite tmxSite,
                                                       MapModel map,
                                                       List<WorldRecordModel> removedWrs,
                                                       ReplayItem? wrOnTmx,
                                                       string subScope)
    {
        var survivingWr = GetCurrentWorldRecord(map);

        var newTime = wrOnTmx?.ReplayTime.TotalMilliseconds;

        // No new world record: either it is the surviving stored one, the surviving one is only backed by an unranked replay,
        // or there are no valid ranked replays at all
        if (newTime is null or <= 0 || (survivingWr is not null && newTime >= survivingWr.Time))
        {
            _logger.LogInformation("Removed world record/s on {mapname}, no new world record replaces them.", map.DeformattedName);

            foreach (var removedWr in removedWrs)
            {
                // Deletes both webhook and bot messages; does nothing for unreported WRs (e.g. older than 14 days)
                await _reportService.RemoveWorldRecordReportAsync(removedWr);
            }

            await _wrUnitOfWork.SaveAsync();

            return;
        }

        var newWr = await AddWorldRecordAsync(tmxSite,
                                              map,
                                              survivingWr,
                                              newTime.Value,
                                              wrOnTmx!.User,
                                              wrOnTmx.ReplayAt.UtcDateTime,
                                              wrOnTmx.ReplayId);

        _logger.LogInformation("Removed world record/s on {mapname}, new world record: {time} by {player}",
            map.DeformattedName, newWr.TimeInt32, newWr.GetPlayerNickname());

        // Always announced, even if the new world record was driven long ago, as the removal is the news
        await _reportService.ReportRemovedWorldRecordsAsync(newWr, removedWrs, GetWorldRecordScope(tmxSite, subScope));

        await _wrUnitOfWork.SaveAsync();
    }

    private static string GetWorldRecordScope(TmxSite tmxSite, string subScope)
    {
        return $"{(tmxSite == TmxSite.Nations ? ScopeTMNESWCOfficialWR : ScopeOfficialWR)}:{subScope}";
    }

    private async ValueTask ReportChangesInRecordsAsync(MapModel map, IEnumerable<TmxReplay> records, IEnumerable<TmxReplay> prevRecords, string subScope, TmxSite tmxSite, bool fullScan)
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

        if (!fullScan && rich.RemovedRecords.Any(x => x.Rank <= ReportService.MaxReportedRemovalRank))
        {
            _logger.LogInformation("Removed leaderboard record found on {mapname}, requesting a full TMX scan.", map.DeformattedName);
            _fullScanRequest.Request();
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

        var wrModel = await AddWorldRecordAsync(tmxSite, map, currentWr, newTimeOrScore, user, replayAt, replayId);

        if (!freshUpdate && DateTime.UtcNow - replayAt < TimeSpan.FromDays(14))
        {
            await _reportService.ReportWorldRecordAsync(wrModel, GetWorldRecordScope(tmxSite, subScope));
        }

        await _wrUnitOfWork.SaveAsync();

        return wrModel;
    }

    private async Task<WorldRecordModel> AddWorldRecordAsync(TmxSite tmxSite,
                                                             MapModel map,
                                                             WorldRecordModel? previousWr,
                                                             int newTimeOrScore,
                                                             User user,
                                                             DateTime replayAt,
                                                             int replayId)
    {
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
            PreviousWorldRecord = previousWr,
            ReplayId = replayId
        };

        await _wrUnitOfWork.WorldRecords.AddAsync(wrModel);

        return wrModel;
    }

    /// <summary>
    /// Requests a full scan once a removed world record is found. The scan then handles removals on all tracks.
    /// </summary>
    /// <returns>True if a full scan was requested.</returns>
    internal async Task<bool> RequestFullScanIfRemovedAsync(IEnumerable<WorldRecordModel> lastTmxWrs)
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

            if (map.Mode?.Id == (int)MapMode.Stunts)
            {
                // TODO
                continue;
            }

            var replays = await _tmxService.GetReplaysAsync(site.GetSiteEnum(), map.MxId.Value);

            if (!IsRemovedFromTmx(wr, replays, map.UpdatedOn))
            {
                continue;
            }

            if (_fullScanRequest.Request(wr.Guid))
            {
                _logger.LogInformation("Removed world record detected on {mapname}: {time} by {player}, requesting a full TMX scan.",
                    map.DeformattedName, wr.TimeInt32, wr.GetPlayerNickname());

                // One removal is enough, the full scan checks all tracks
                return true;
            }

            _logger.LogWarning("Removed world record on {mapname}: {time} by {player} already triggered a full TMX scan, not requesting another one.",
                map.DeformattedName, wr.TimeInt32, wr.GetPlayerNickname());
        }

        return false;
    }

    /// <summary>
    /// A world record is considered removed if no replay on TMX is at least as fast.
    /// Unranked replays count too, so a track update (which unranks all existing replays) is not mistaken for a removal.
    /// An empty response is not considered a removal, as it can also be a TMX glitch.
    /// </summary>
    private bool IsRemovedFromTmx(WorldRecordModel wr, ItemCollection<ReplayItem> replays, DateTimeOffset trackAt)
    {
        var fastestOnTmx = _tmxService.GetWrHistory(trackAt, replays).LastOrDefault();

        return fastestOnTmx is not null && fastestOnTmx.ReplayTime.TotalMilliseconds > wr.Time;
    }

    /// <summary>
    /// Ignores all stored world records that are no longer on TMX, starting from the current one.
    /// Their reports are not touched yet, that depends on whether a new world record replaces them.
    /// </summary>
    /// <returns>Removed world records, the most recent one first.</returns>
    private List<WorldRecordModel> IgnoreRemovedWorldRecords(MapModel map,
                                                             WorldRecordModel? currentWr,
                                                             TrackItem tmxTrack,
                                                             ItemCollection<ReplayItem> replays)
    {
        var removedWrs = new List<WorldRecordModel>();

        while (currentWr is not null && IsRemovedFromTmx(currentWr, replays, tmxTrack.UpdatedAt))
        {
            _logger.LogInformation("Removed WR on {mapname}: {time} by {player}",
                map.DeformattedName, currentWr.TimeInt32, currentWr.GetPlayerNickname());

            currentWr.Ignored = IgnoredMode.Ignored;
            removedWrs.Add(currentWr);

            currentWr = GetCurrentWorldRecord(map);
        }

        return removedWrs;
    }

    private static WorldRecordModel? GetCurrentWorldRecord(MapModel map)
    {
        return map.WorldRecords
            .Where(x => x.Ignored == IgnoredMode.NotIgnored)
            .OrderByDescending(x => x.DrivenOn)
            .FirstOrDefault();
    }
}
