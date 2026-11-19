using System.Collections.Concurrent;

namespace BigBang1112.WorldRecordReportLib.Services;

/// <summary>
/// Signals the TMX refresh job to check all tracks instead of only the ones with new activity.
/// Requested when a removed world record is detected, as the removal (e.g. of a cheater's replays)
/// can affect other tracks without causing any new activity on them.
/// The scan runs inside the refresh job, so it never processes the same track concurrently with it.
/// Kept in memory only, a pending request is lost on restart.
/// </summary>
public class TmxFullScanRequest
{
    private int _requested;

    // World records that already triggered a full scan, so a removal the scan cannot resolve
    // (e.g. the track is not part of any scanned leaderboard) does not trigger a full scan on every cleanup
    private readonly ConcurrentDictionary<Guid, byte> _triggeredBy = new();

    public void Request()
    {
        Interlocked.Exchange(ref _requested, 1);
    }

    /// <returns>False if this world record has already triggered a full scan before, nothing is requested then.</returns>
    public bool Request(Guid removedWorldRecordGuid)
    {
        if (!_triggeredBy.TryAdd(removedWorldRecordGuid, 0))
        {
            return false;
        }

        Request();

        return true;
    }

    /// <returns>True if a full scan was requested. The request is cleared.</returns>
    public bool TryConsume()
    {
        return Interlocked.Exchange(ref _requested, 0) == 1;
    }
}
