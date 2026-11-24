using System.Collections.Concurrent;
using ManiaAPI.TMX;

using TmxSite = BigBang1112.WorldRecordReportLib.Enums.TmxSite;

namespace BigBang1112.WorldRecordReportLib.Services;

/// <summary>
/// Remembers TMX tracks whose new activity failed to process, so they can be retried on later runs.
/// The activity scan stops at the first already-handled track, so a failed track would otherwise never be reached again.
/// Kept in memory only, the queue is lost on restart.
/// </summary>
public class TmxFailedTrackQueue
{
    public const int MaxAttempts = 3;

    private readonly ConcurrentDictionary<(TmxSite Site, string SubScope, long TrackId), FailedTmxTrack> _tracks = new();

    public IReadOnlyList<FailedTmxTrack> GetAll(TmxSite site, string subScope)
    {
        return _tracks
            .Where(x => x.Key.Site == site && x.Key.SubScope == subScope)
            .Select(x => x.Value)
            .ToList();
    }

    /// <summary>
    /// Counts a failed attempt. The track is dropped from the queue once it reaches <see cref="MaxAttempts"/>.
    /// </summary>
    /// <returns>Number of failed attempts so far, including this one.</returns>
    public int RegisterFailure(TmxSite site, string subScope, TrackItem track, bool addMissingClassicTrack = false)
    {
        var key = (site, subScope, (long)track.TrackId);

        // Keep the latest track item so the retry uses the freshest activity date
        var failed = _tracks.AddOrUpdate(key,
            _ => new FailedTmxTrack(track, Attempts: 1, addMissingClassicTrack),
            (_, existing) => new FailedTmxTrack(track, existing.Attempts + 1,
                addMissingClassicTrack || existing.AddMissingClassicTrack));

        if (failed.Attempts >= MaxAttempts)
        {
            _tracks.TryRemove(key, out _);
        }

        return failed.Attempts;
    }

    public void Remove(TmxSite site, string subScope, long trackId)
    {
        _tracks.TryRemove((site, subScope, trackId), out _);
    }
}

public sealed record FailedTmxTrack(TrackItem Track, int Attempts, bool AddMissingClassicTrack = false);
