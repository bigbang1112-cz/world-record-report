using BigBang1112.WorldRecordReportLib.Services;
using Quartz;

namespace BigBang1112.WorldRecordReportLib.Jobs;

public class RefreshTM2020CurrentCampaignJob : IJob
{
    private readonly RefreshTM2020Service _refreshService;

    public RefreshTM2020CurrentCampaignJob(RefreshTM2020Service refreshService)
    {
        _refreshService = refreshService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await _refreshService.RefreshCurrentCampaignAsync(forceUpdate: false);
    }
}
