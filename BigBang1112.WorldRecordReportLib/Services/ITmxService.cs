using TmExchangeApi;
using TmExchangeApi.Models;

namespace BigBang1112.WorldRecordReportLib.Services;

public interface ITmxService
{
    Task<ItemCollection<ReplayItem>> GetReplaysAsync(TmxSite site, int tmxId, CancellationToken cancellationToken = default);
    Task<ItemCollection<TrackSearchItem>> SearchAsync(TmxSite site, TrackSearchFilters trackSearchFilters, CancellationToken cancellationToken = default);
    IEnumerable<ReplayItem> GetWrHistory(ItemCollection<ReplayItem> replays, bool isStunts = false);
}
