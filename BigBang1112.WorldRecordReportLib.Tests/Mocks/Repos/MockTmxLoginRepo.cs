using BigBang1112.Repos.Mocks;
using BigBang1112.WorldRecordReportLib.Enums;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;

namespace BigBang1112.WorldRecordReportLib.Tests.Mocks.Repos;

public class MockTmxLoginRepo : MockRepo<TmxLoginModel>, ITmxLoginRepo
{
    public Task<Dictionary<int, TmxLoginModel>> GetByUserIdsAsync(IEnumerable<int> userIds, TmxSite tmxSite, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<TmxLoginModel> GetOrAddAsync(int userId, TmxSite tmxSite, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
