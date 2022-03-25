using TmEssentials;

namespace BigBang1112.WorldRecordReportLib.Models;

public class TmxReplay
{
    public int ReplayId { get; init; }
    public int ReplayTime { get; init; }
    public int ReplayScore { get; init; }
    public int ReplayRespawns { get; init; }
    public DateTime ReplayAt { get; init; }
    public int? Rank { get; init; } // Position + 1
    public int IsBest { get; init; }
    public int? Score { get; init; }
    public bool IsCompPatch { get; init; } // Validated
    public int UserId { get; init; } // User.UserId
    public string? UserName { get; init; }

    public override string ToString()
    {
        return $"{Rank?.ToString() ?? "-"}) {new TimeInt32(ReplayTime)} by {UserId} ({ReplayAt})";
    }
}
