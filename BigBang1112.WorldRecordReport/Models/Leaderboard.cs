namespace BigBang1112.WorldRecordReport.Models;

public class Leaderboard
{
    public string MapUid { get; init; } = default!;
    public IEnumerable<LeaderboardRecord> Records { get; init; } = default!;
}
