using ManiaAPI.TMX;

namespace BigBang1112.WorldRecordReportLib.Services;

public class TmxService : ITmxService
{
    private readonly IHttpClientFactory _httpFactory;

    public TmxService(IHttpClientFactory httpFactory)
    {
        _httpFactory = httpFactory;
    }

    public async Task<ItemCollection<ReplayItem>> GetReplaysAsync(TmxSite site, long tmxId, CancellationToken cancellationToken = default)
    {
        var http = _httpFactory.CreateClient("resilient");
        var tmx = new TMX(http, site);
        return await tmx.GetReplaysAsync(new() { TrackId = tmxId }, cancellationToken);
    }

    public IEnumerable<ReplayItem> GetWrHistory(DateTimeOffset trackAt, ItemCollection<ReplayItem> replays, bool isStunts = false)
    {
        var tempTime = default(int?);

        foreach (var replay in replays.Results.OrderBy(x => x.ReplayAt).Where(x => x.TrackAt == trackAt))
        {
            if (tempTime is null)
            {
                tempTime = isStunts ? replay.ReplayScore : replay.ReplayTime.TotalMilliseconds;
                yield return replay;
            }

            if (isStunts && replay.ReplayScore > tempTime)
            {
                tempTime = replay.ReplayScore;
                yield return replay;
            }

            if (!isStunts && replay.ReplayTime.TotalMilliseconds < tempTime)
            {
                tempTime = replay.ReplayTime.TotalMilliseconds;
                yield return replay;
            }
        }
    }

    public async Task<ItemCollection<TrackItem>> SearchAsync(TmxSite site, TMX.SearchTracksParameters trackSearchFilters, CancellationToken cancellationToken = default)
    {
        var http = _httpFactory.CreateClient("resilient");
        var tmx = new TMX(http, site);
        return await tmx.SearchTracksAsync(trackSearchFilters, cancellationToken);
    }
}
