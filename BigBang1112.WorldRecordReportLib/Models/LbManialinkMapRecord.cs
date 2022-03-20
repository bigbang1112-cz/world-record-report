using Mapster;

namespace BigBang1112.WorldRecordReportLib.Models;

public class LbManialinkMapRecord
{
    public int Rank { get; init; }
    public int Time { get; init; }
    public string Login { get; init; } = default!;
    public string Nickname { get; init; } = default!;
    public int Timestamp { get; init; }
    public string ReplayUrl { get; init; } = default!;
}
