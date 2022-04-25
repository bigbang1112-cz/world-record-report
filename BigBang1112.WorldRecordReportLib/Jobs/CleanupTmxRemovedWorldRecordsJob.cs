using BigBang1112.WorldRecordReportLib.Services;
using Quartz;

namespace BigBang1112.WorldRecordReportLib.Jobs;

[DisallowConcurrentExecution]
public class CleanupTmxRemovedWorldRecordsJob : IJob
{
    private readonly RefreshTmxService tmxService;

    public CleanupTmxRemovedWorldRecordsJob(RefreshTmxService tmxService)
    {
        this.tmxService = tmxService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await tmxService.CleanupRemovedWorldRecordsAsync();
    }
}
