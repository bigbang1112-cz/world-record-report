using EFCoreSecondLevelCacheInterceptor;
using Microsoft.EntityFrameworkCore;

namespace BigBang1112.WorldRecordReportLib.Repos;

public class ReportRepo : Repo<ReportModel>, IReportRepo
{
    private readonly WrContext _context;

    public ReportRepo(WrContext context) : base(context)
    {
        _context = context;
    }

    public async Task<ReportModel?> GetByWorldRecordAsync(WorldRecordModel wr, CancellationToken cancellationToken = default)
    {
        return await _context.Reports.FirstOrDefaultAsync(x => x.WorldRecord == wr, cancellationToken);
    }
}
