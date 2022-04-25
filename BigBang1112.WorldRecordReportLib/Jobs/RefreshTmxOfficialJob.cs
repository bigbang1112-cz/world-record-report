using BigBang1112.WorldRecordReportLib.Enums;
using BigBang1112.WorldRecordReportLib.Services;
using ManiaAPI.TMX;
using Quartz;

using TmxSite = BigBang1112.WorldRecordReportLib.Enums.TmxSite;

namespace BigBang1112.WorldRecordReportLib.Jobs;

[DisallowConcurrentExecution]
public class RefreshTmxOfficialJob : IJob
{
    private readonly RefreshTmxService _tmx;

    public RefreshTmxOfficialJob(RefreshTmxService tmx)
    {
        _tmx = tmx;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await _tmx.UpdateWorldRecordsAsync(TmxSite.United, LeaderboardType.Nadeo);
        await _tmx.UpdateWorldRecordsAsync(TmxSite.United, LeaderboardType.Star);
        await _tmx.UpdateWorldRecordsAsync(TmxSite.TMNF, LeaderboardType.Nadeo);
    }
}
