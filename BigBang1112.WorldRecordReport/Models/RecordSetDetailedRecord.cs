using System.Text.Json.Serialization;
using TmEssentials;

namespace BigBang1112.WorldRecordReport.Models;

public class RecordSetDetailedRecord
{
    public int Rank { get; init; }
    public string Login { get; init; }
    public int Time { get; init; }
    public string? ReplayUrl { get; init; }

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
