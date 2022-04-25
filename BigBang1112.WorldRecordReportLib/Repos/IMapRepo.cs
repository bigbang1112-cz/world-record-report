using BigBang1112.WorldRecordReportLib.Enums;

namespace BigBang1112.WorldRecordReportLib.Repos;

public interface IMapRepo : IRepo<MapModel>
{
    Task<MapModel?> GetByUidAsync(string mapUid, CancellationToken cancellationToken = default);
    Task<Guid?> GetMapIdByMapUidAsync(string mapUid, CancellationToken cancellationToken = default);
    Task<List<MapModel>> GetByCampaignAsync(CampaignModel campaign, CancellationToken cancellationToken = default);
    Task<MapModel?> GetByMxIdAsync(int trackId, TmxSite tmxSite, CancellationToken cancellationToken = default);
}
