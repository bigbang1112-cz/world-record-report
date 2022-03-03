using BigBang1112.WorldRecordReport.Services;
using Quartz;

namespace BigBang1112.WorldRecordReport.Jobs;

[DisallowConcurrentExecution]
public class RefreshTmxNationsForeverNadeoJob : IJob
{
    private readonly TmxService _tmx;

    public RefreshTmxNationsForeverNadeoJob(TmxService tmx)
    {
        _tmx = tmx;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await _tmx.UpdateWorldRecordsAsync(TmExchangeApi.TmxSite.TMNForever);
    }
}
