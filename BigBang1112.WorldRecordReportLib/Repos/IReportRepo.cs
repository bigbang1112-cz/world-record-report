namespace BigBang1112.WorldRecordReportLib.Repos;

public interface IReportRepo : IRepo<ReportModel>
{
    Task<ReportModel?> GetByWorldRecordAsync(WorldRecordModel wr, CancellationToken cancellationToken = default);
}
