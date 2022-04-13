using TmEssentials;

namespace BigBang1112.WorldRecordReportLib.Models;

public record struct TM2020RecordFundamental : IRecord<Guid>
{
    public int Rank { get; init; }
    public Guid AccountId { get; init; }
    public TimeInt32 Score { get; init; }

    Guid IRecord<Guid>.PlayerId { get => AccountId; init => AccountId = value; }
    int IRecord<Guid>.Time { get => Score.TotalMilliseconds; init => Score = new TimeInt32(value); }
}
