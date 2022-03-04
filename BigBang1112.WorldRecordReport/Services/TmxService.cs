using TmExchangeApi;
using TmExchangeApi.Models;

namespace BigBang1112.WorldRecordReport.Services;

public class TmxService : ITmxService
{
    public async Task<ItemCollection<ReplayItem>> GetReplaysAsync(TmxSite site, int tmxId, CancellationToken cancellationToken = default)
    {
        var tmx = new Tmx(site);
        return await tmx.GetReplaysAsync(tmxId, cancellationToken);
    }

    public IEnumerable<ReplayItem> GetWrHistory(ItemCollection<ReplayItem> replays, bool isStunts = false)
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

    public async Task<ItemCollection<TrackSearchItem>> SearchAsync(TmxSite site, TrackSearchFilters trackSearchFilters, CancellationToken cancellationToken = default)
    {
        var tmx = new Tmx(site);
        return await tmx.SearchAsync(trackSearchFilters, cancellationToken);
    }
}
