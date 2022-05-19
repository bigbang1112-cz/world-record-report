using TmEssentials;

namespace BigBang1112.WorldRecordReportLib.Models;

public record TM2Record(int Rank,
                        string Login,
                        TimeInt32 Time,
                        string? DisplayName = null,
                        string? ReplayUrl = null,
                        DateTimeOffset? Timestamp = null) : IRecord<string>
{
    string IRecord<string>.PlayerId
    {
        get => Login;
        init => Login = value;
    }

    int? IRecord.Rank
    {
        get => Rank;
        init => Rank = value.GetValueOrDefault();
    }

    public string GetDisplayNameMdLink()
    {
        var name = DisplayName is null
            ? Login
            : TextFormatter.Deformat(DisplayName);

        return name.EscapeDiscord();
    }

    public string GetPlayerId()
    {
        return Login;
    }

    public override string ToString()
    {
        return $"{Rank}) {Time} by {Login}";
    }
}
