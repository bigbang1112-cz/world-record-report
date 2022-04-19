using TmEssentials;

namespace BigBang1112.WorldRecordReportLib.Models;

public class TmxReplay : IRecord<int>
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
    
    int IRecord<int>.PlayerId
    {
        get => UserId;
        init => UserId = value;
    }
    
    TimeInt32 IRecord.Time
    {
        get => new(ReplayTime);
        init => ReplayTime = value.TotalMilliseconds;
    }
    
    string? IRecord.DisplayName
    {
        get => UserName;
        init => UserName = value;
    }

    public override string ToString()
    {
        return $"{Rank?.ToString() ?? "-"}) {new TimeInt32(ReplayTime)} by {UserId} ({ReplayAt})";
    }
}
