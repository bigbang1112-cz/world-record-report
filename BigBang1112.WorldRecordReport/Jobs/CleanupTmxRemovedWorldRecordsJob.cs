using BigBang1112.WorldRecordReport.Services;
using Quartz;

namespace BigBang1112.WorldRecordReport.Jobs;

[DisallowConcurrentExecution]
public class CleanupTmxRemovedWorldRecordsJob : IJob
{
    private readonly TmxReportService tmxService;

    public CleanupTmxRemovedWorldRecordsJob(TmxReportService tmxService)
    {
        this.tmxService = tmxService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await tmxService.CleanupRemovedWorldRecordsAsync();
    }
}
