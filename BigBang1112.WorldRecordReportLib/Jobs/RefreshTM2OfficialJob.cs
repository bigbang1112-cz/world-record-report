using BigBang1112.WorldRecordReportLib.Services;
using Quartz;

namespace BigBang1112.WorldRecordReportLib.Jobs;

[DisallowConcurrentExecution]
public class RefreshTM2OfficialJob : IJob
{
    private readonly RefreshTM2Service _worldRecordTM2Service;

    public RefreshTM2OfficialJob(RefreshTM2Service worldRecordTM2Service)
    {
        _worldRecordTM2Service = worldRecordTM2Service;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await _worldRecordTM2Service.RefreshOfficialAsync();
    }
}
