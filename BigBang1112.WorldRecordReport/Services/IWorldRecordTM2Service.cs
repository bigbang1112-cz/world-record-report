using BigBang1112.WorldRecordReport.Models;
using BigBang1112.WorldRecordReport.Models.Db;

namespace BigBang1112.WorldRecordReport.Services
{
    public interface IWorldRecordTM2Service
    {
        Task HandleLeaderboardAsync(MapModel map, Leaderboard leaderboard, bool isManialinkReport);
        Task HandleLeaderboardAsync(MapModel map, Leaderboard leaderboard, List<WorldRecordModel> newWrsToReport, List<RemovedWorldRecord> removedWrsToReport, bool isManialinkReport);
        Task HandleLeaderboardAsync(string mapUid, Leaderboard leaderboard, bool isManialinkReport);
        Task RefreshWorldRecordsAsync(DateTime fireTime);
    }
}