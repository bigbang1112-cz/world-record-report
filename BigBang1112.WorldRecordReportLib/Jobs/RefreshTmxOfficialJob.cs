using BigBang1112.WorldRecordReportLib.Services;
using ManiaAPI.TMX;
using Quartz;

namespace BigBang1112.WorldRecordReportLib.Jobs;

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
        await _tmx.UpdateWorldRecordsAsync(TmxSite.United, LeaderboardType.Nadeo);
        await _tmx.UpdateWorldRecordsAsync(TmxSite.United, LeaderboardType.Star);
        await _tmx.UpdateWorldRecordsAsync(TmxSite.TMNForever, LeaderboardType.Nadeo);
    }
}
