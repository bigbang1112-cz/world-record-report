using Quartz;

namespace BigBang1112.WorldRecordReportLib.Jobs;

[DisallowConcurrentExecution]
public class AcquireNewOfficialCampaignsJob : IJob
{
    public Task Execute(IJobExecutionContext context)
    {
        return Task.CompletedTask;
        //throw new NotImplementedException();
    }
}
