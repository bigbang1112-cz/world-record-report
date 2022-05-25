using BigBang1112.Repos.Mocks;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;

namespace BigBang1112.WorldRecordReportLib.Tests.Mocks.Repos;

public class MockReportRepo : MockRepo<ReportModel>, IReportRepo
{
    public Task<ReportModel?> GetByWorldRecordAsync(WorldRecordModel wr, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Entities.FirstOrDefault(x => x.WorldRecord == wr));
    }
}
