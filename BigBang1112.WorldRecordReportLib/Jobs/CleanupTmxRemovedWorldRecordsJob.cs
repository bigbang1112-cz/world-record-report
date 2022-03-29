using BigBang1112.WorldRecordReportLib.Services;
using Quartz;

namespace BigBang1112.WorldRecordReportLib.Jobs;

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
