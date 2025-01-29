using ManiaAPI.TMX;

namespace BigBang1112.WorldRecordReportLib.Services;

public interface ITmxService
{
    Task<ItemCollection<ReplayItem>> GetReplaysAsync(TmxSite site, long tmxId, CancellationToken cancellationToken = default);
    Task<ItemCollection<TrackItem>> SearchAsync(TmxSite site, TMX.SearchTracksParameters trackSearchFilters, CancellationToken cancellationToken = default);
    IEnumerable<ReplayItem> GetWrHistory(ItemCollection<ReplayItem> replays, bool isStunts = false);

    async Task<ItemCollection<ReplayItem>> GetReplaysAsync(TmxSiteModel site, long tmxId, CancellationToken cancellationToken = default)
    {
        return await GetReplaysAsync(site.GetSiteEnum(), tmxId, cancellationToken);
    }
}
