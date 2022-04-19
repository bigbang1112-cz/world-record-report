using TmEssentials;

namespace BigBang1112.WorldRecordReportLib.Models;

/// <summary>
/// General record in a leaderboard.
/// </summary>
public interface IRecord
{
    int? Rank { get; init; }
    TimeInt32 Time { get; init; }
    string? DisplayName { get; init; }
}
