using BigBang1112.WorldRecordReportLib.Enums;
using BigBang1112.WorldRecordReportLib.Services;
using Quartz;

namespace BigBang1112.WorldRecordReportLib.Jobs;

public class RefreshTM2020PreviousCampaignsJob : IJob
{
    private readonly RefreshTM2020Service _refreshService;
    private readonly RefreshScheduleService _refreshScheduleService;
    private readonly IWrUnitOfWork _wrUnitOfWork;

    public RefreshTM2020PreviousCampaignsJob(RefreshTM2020Service refreshService,
                                             RefreshScheduleService refreshScheduleService,
                                             IWrUnitOfWork wrUnitOfWork)
    {
        _refreshService = refreshService;
        _refreshScheduleService = refreshScheduleService;
        _wrUnitOfWork = wrUnitOfWork;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        if (_refreshScheduleService.TM2020PreviousCampaignsMapCycle is null
         && _refreshScheduleService.TM2020CurrentCampaignMapCycle is not null)
        {
            _refreshScheduleService.SetupTM2020PreviousCampaigns(
                await _wrUnitOfWork.Maps.GetByCampaignsThatAreOverAsync(Game.TM2020));
        }

        await _refreshService.RefreshPreviousCampaignsAsync(forceUpdate: false);
    }
}
