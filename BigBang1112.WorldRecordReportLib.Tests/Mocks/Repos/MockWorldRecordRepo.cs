using BigBang1112.Repos.Mocks;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;

namespace BigBang1112.WorldRecordReportLib.Tests.Mocks.Repos;

public class MockWorldRecordRepo : MockRepo<WorldRecordModel>, IWorldRecordRepo
{
    public async Task<WorldRecordModel?> GetCurrentByMapUidAsync(string mapUid, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(Entities.SingleOrDefault(x => x.Map.MapUid == mapUid));
    }
}
