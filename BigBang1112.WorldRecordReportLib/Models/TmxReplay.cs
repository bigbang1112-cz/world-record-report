using BigBang1112.WorldRecordReportLib.Enums;
using TmEssentials;

namespace BigBang1112.WorldRecordReportLib.Models;

public class TmxReplay : IRecord<int>
{
    public int ReplayId { get; init; }
    public TimeInt32 ReplayTime { get; init; }
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
        get => ReplayTime;
        init => ReplayTime = value;
    }
    
    string? IRecord.DisplayName
    {
        get => UserName;
        init => UserName = value;
    }

    public string GetDisplayNameMdLink()
    {
        // It's hard to pass the site url to this one
        return UserName?.EscapeDiscord() ?? UserId.ToString();
    }

    public string GetDisplayNameMdLink(TmxSite tmxSite)
    {
        var site = tmxSite switch
        {
            TmxSite.United => "united",
            TmxSite.TMNF => "tmnforever",
            _ => null
        };

        if (site is null)
        {
            return GetDisplayNameMdLink();
        }

        return $"[{UserName ?? UserId.ToString()}](https://{site}.tm-exchange.com/usershow/{UserId})";
    }

    public string GetPlayerId()
    {
        return UserId.ToString();
    }

    public override string ToString()
    {
        return $"{Rank?.ToString() ?? "-"}) {ReplayTime} by {UserName} ({ReplayAt})";
    }
}
