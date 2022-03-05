using BigBang1112.WorldRecordReportLib.Models;
using BigBang1112.WorldRecordReportLib.Models.Db;

namespace BigBang1112.WorldRecordReportLib.Services;

public interface ITM2ReportService
{
    Task HandleLeaderboardAsync(MapModel map, Leaderboard leaderboard, bool isManialinkReport);
    Task HandleLeaderboardAsync(MapModel map, Leaderboard leaderboard, List<WorldRecordModel> newWrsToReport, List<RemovedWorldRecord> removedWrsToReport, bool isManialinkReport);
    Task HandleLeaderboardAsync(string mapUid, Leaderboard leaderboard, bool isManialinkReport);
    Task RefreshWorldRecordsAsync(DateTime fireTime);
}
