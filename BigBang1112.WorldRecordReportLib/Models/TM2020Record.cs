using TmEssentials;

namespace BigBang1112.WorldRecordReportLib.Models;

public record TM2020Record : IRecord<Guid>
{
    public int Rank { get; init; }
    public Guid PlayerId { get; init; }
    public TimeInt32 Time { get; init; }
    public string? DisplayName { get; init; }
    public string? GhostUrl { get; init; }
    public DateTime Timestamp { get; init; }
    public bool Ignored { get; init; }

    int? IRecord.Rank
    {
        get => Rank;
        init => Rank = value.GetValueOrDefault();
    }

    public string GetDisplayNameMdLink()
    {
        return $"[{DisplayName?.EscapeDiscord() ?? PlayerId.ToString()}](https://trackmania.io/#/player/{PlayerId})";
    }

    public override string ToString()
    {
        return $"{Time} by {DisplayName ?? PlayerId.ToString()}";
    }
}
