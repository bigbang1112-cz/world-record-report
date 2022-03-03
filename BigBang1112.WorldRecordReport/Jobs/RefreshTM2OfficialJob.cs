using BigBang1112.WorldRecordReport.Services;
using Quartz;

namespace BigBang1112.WorldRecordReport.Jobs;

[DisallowConcurrentExecution]
public class RefreshTM2OfficialJob : IJob
{
    private readonly IWorldRecordTM2Service _worldRecordTM2Service;

    public RefreshTM2OfficialJob(IWorldRecordTM2Service worldRecordTM2Service)
    {
        _worldRecordTM2Service = worldRecordTM2Service;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await _worldRecordTM2Service.RefreshWorldRecordsAsync(context.FireTimeUtc.UtcDateTime);
    }
}
