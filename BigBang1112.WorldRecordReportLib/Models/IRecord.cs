namespace BigBang1112.WorldRecordReportLib.Models;

/// <summary>
/// General record in a leaderboard.
/// </summary>
/// <typeparam name="T">Player ID type.</typeparam>
public interface IRecord<T> where T : notnull
{
    T PlayerId { get; init; }
    int Time { get; init; }
}
