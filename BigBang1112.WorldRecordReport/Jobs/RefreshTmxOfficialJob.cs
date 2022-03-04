using BigBang1112.WorldRecordReport.Services;
using Quartz;

namespace BigBang1112.WorldRecordReport.Jobs;

[DisallowConcurrentExecution]
public class RefreshTmxOfficialJob : IJob
{
    private readonly TmxReportService _tmx;

    public RefreshTmxOfficialJob(TmxReportService tmx)
    {
        _tmx = tmx;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await _tmx.UpdateWorldRecordsAsync(TmExchangeApi.TmxSite.United, TmExchangeApi.LeaderboardType.Nadeo);
        await _tmx.UpdateWorldRecordsAsync(TmExchangeApi.TmxSite.United, TmExchangeApi.LeaderboardType.Star);
        await _tmx.UpdateWorldRecordsAsync(TmExchangeApi.TmxSite.TMNForever, TmExchangeApi.LeaderboardType.Nadeo);
    }
}
