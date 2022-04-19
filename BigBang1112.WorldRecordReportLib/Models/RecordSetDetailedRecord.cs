using TmEssentials;

namespace BigBang1112.WorldRecordReportLib.Models;

public class RecordSetDetailedRecord : IRecord<string>
{
    public int Rank { get; init; }
    public string Login { get; init; }
    public int Time { get; init; }
    public string? ReplayUrl { get; init; }
    
    string IRecord<string>.PlayerId
    {
        get => Login;
        init => Login = value;
    }
    
    TimeInt32 IRecord.Time
    {
        get => new(Time);
        init => Time = value.TotalMilliseconds;
    }

    public string? DisplayName { get; init; }

    int? IRecord.Rank
    {
        get => Rank;
        init => Rank = value.GetValueOrDefault();
    }

    public RecordSetDetailedRecord(int rank, string login, int time, string? replayUrl = null)
    {
        Rank = rank;
        Login = login;
        Time = time;
        ReplayUrl = replayUrl;
    }

    public override string ToString()
    {
        return $"{Rank}) {new TimeInt32(Time)} by {Login}";
    }
}
