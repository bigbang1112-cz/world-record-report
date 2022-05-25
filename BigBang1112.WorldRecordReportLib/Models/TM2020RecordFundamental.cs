using TmEssentials;

namespace BigBang1112.WorldRecordReportLib.Models;

public record struct TM2020RecordFundamental : IRecord<Guid>
{
    public int? Rank { get; init; }
    public Guid AccountId { get; init; }
    public TimeInt32 Score { get; init; }
    
    Guid IRecord<Guid>.PlayerId { get => AccountId; init => AccountId = value; }
    TimeInt32 IRecord.Time { get => Score; init => Score = value; }
    string? IRecord.DisplayName { get; init; }

    public string GetDisplayNameMdLink()
    {
        return (this as IRecord).DisplayName ?? AccountId.ToString();
    }

    public string GetPlayerId()
    {
        return AccountId.ToString();
    }
}
