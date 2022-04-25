using BigBang1112.Repos.Mocks;
using BigBang1112.WorldRecordReportLib.Enums;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;

namespace BigBang1112.WorldRecordReportLib.Tests.Mocks.Repos;

public class MockMapRepo : MockRepo<MapModel>, IMapRepo
{
    public async Task<List<MapModel>> GetByCampaignAsync(CampaignModel campaign, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(Entities.Where(x => x.Campaign == campaign).ToList());
    }

    public Task<MapModel?> GetByMxIdAsync(int trackId, TmxSite tmxSite, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<MapModel?> GetByUidAsync(string mapUid, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(Entities.SingleOrDefault(x => x.MapUid == mapUid));
    }

    public async Task<Guid?> GetMapIdByMapUidAsync(string mapUid, CancellationToken cancellationToken = default)
    {
        return (await GetByUidAsync(mapUid, cancellationToken))?.MapId;
    }
}
