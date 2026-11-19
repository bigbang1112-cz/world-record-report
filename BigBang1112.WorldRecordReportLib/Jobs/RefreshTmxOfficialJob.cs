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
    private readonly TmxFullScanRequest _fullScanRequest;

    public RefreshTmxOfficialJob(RefreshTmxService tmx, TmxFullScanRequest fullScanRequest)
    {
        _tmx = tmx;
        _fullScanRequest = fullScanRequest;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var fullScan = _fullScanRequest.TryConsume();

        try
        {
            await _tmx.UpdateWorldRecordsAsync(TmxSite.United, LeaderboardType.Nadeo, "NadeoTMUF", fullScan);
            await _tmx.UpdateWorldRecordsAsync(TmxSite.United, LeaderboardType.Star, "StarTrack", fullScan);
            await _tmx.UpdateWorldRecordsAsync(TmxSite.TMNF, LeaderboardType.Nadeo, "NadeoTMNF", fullScan);
            await _tmx.UpdateWorldRecordsAsync(TmxSite.United, LeaderboardType.Classic, "ClassicTMUF", fullScan);
            await _tmx.UpdateWorldRecordsAsync(TmxSite.TMNF, LeaderboardType.Classic, "ClassicTMNF", fullScan);
            await _tmx.UpdateWorldRecordsAsync(TmxSite.Nations, LeaderboardType.Nadeo, "Nadeo", fullScan);
            await _tmx.UpdateWorldRecordsAsync(TmxSite.Nations, LeaderboardType.Classic, "Classic", fullScan);
        }
        catch
        {
            // An interrupted full scan has to be done again from the start
            if (fullScan)
            {
                _fullScanRequest.Request();
            }

            throw;
        }
    }
}
