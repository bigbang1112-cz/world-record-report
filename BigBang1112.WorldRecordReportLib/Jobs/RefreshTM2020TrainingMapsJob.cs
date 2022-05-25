using BigBang1112.WorldRecordReportLib.Enums;
using BigBang1112.WorldRecordReportLib.Services;
using Quartz;

namespace BigBang1112.WorldRecordReportLib.Jobs;

public class RefreshTM2020TrainingMapsJob : IJob
{
    private readonly RefreshTM2020Service _refreshService;
    private readonly RefreshScheduleService _refreshScheduleService;
    private readonly IWrUnitOfWork _wrUnitOfWork;

    public RefreshTM2020TrainingMapsJob(RefreshTM2020Service refreshService,
                                        RefreshScheduleService refreshScheduleService,
                                        IWrUnitOfWork wrUnitOfWork)
    {
        _refreshService = refreshService;
        _refreshScheduleService = refreshScheduleService;
        _wrUnitOfWork = wrUnitOfWork;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        if (_refreshScheduleService.TM2020TrainingMapCycle is null)
        {
            var maps = await _wrUnitOfWork.Maps.GetAllByCampaignLeaderboardUidAsync("NLS-QgdzyWx3sNU7IOGuJGKVBKkps6rMiNTGesM");

            if (maps.Count() >= 25)
            {
                _refreshScheduleService.SetupTM2020TrainingMaps(maps);
            }
        }

        await _refreshService.RefreshTrainingMapsAsync(forceUpdate: false);
    }
}
