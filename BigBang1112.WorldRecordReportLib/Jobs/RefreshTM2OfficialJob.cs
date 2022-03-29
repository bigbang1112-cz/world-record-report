using BigBang1112.WorldRecordReportLib.Services;
using Quartz;

namespace BigBang1112.WorldRecordReportLib.Jobs;

[DisallowConcurrentExecution]
public class RefreshTM2OfficialJob : IJob
{
    private readonly ITM2ReportService _worldRecordTM2Service;

    public RefreshTM2OfficialJob(ITM2ReportService worldRecordTM2Service)
    {
        _worldRecordTM2Service = worldRecordTM2Service;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await _worldRecordTM2Service.RefreshWorldRecordsAsync(context.FireTimeUtc.UtcDateTime);
    }
}
