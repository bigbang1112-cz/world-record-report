using Mapster;

namespace BigBang1112.WorldRecordReport.Models;

public class LbManialinkMapRecord
{
    public int Rank { get; init; }
    public int Time { get; init; }
    public string Login { get; init; } = default!;
    public string Nickname { get; init; } = default!;
    public int Timestamp { get; init; }
    public string ReplayUrl { get; init; } = default!;

    static LbManialinkMapRecord()
    {
        TypeAdapterConfig<LbManialinkMapRecord, LeaderboardRecord>
            .ForType()
            .Map(dest => dest.Time, src => TimeSpan.FromMilliseconds(src.Time))
            .Map(dest => dest.Timestamp, src => DateTimeOffset.FromUnixTimeSeconds(src.Timestamp))
            .Map(dest => dest.IsFromManialink, src => true);
    }
}
