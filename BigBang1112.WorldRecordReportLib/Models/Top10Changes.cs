namespace BigBang1112.WorldRecordReportLib.Models;

public class Top10Changes<TPlayerId> where TPlayerId : notnull
{
    /// <summary>
    /// Contains new record occurances, where <see cref="IRecord{T}.Time"/> is the time of this record. Rest of the properties should also relate to this new record.
    /// In limited range leaderboards, this will often connect with either <see cref="PushedOffRecords"/> or <see cref="RemovedRecords"/>.
    /// </summary>
    public IEnumerable<IRecord<TPlayerId>> NewRecords { get; init; }
    /// <summary>
    /// Contains previous records before they were improved. <see cref="IRecord{T}.Time"/> is the time of the previous record. Rest of the properties should also relate to the previous record.
    /// To get the improved record, try fetching either newer change by the player, or if none are found, use the current leaderboard data.
    /// </summary>
    public IEnumerable<IRecord<TPlayerId>> ImprovedRecords { get; init; }
    /// <summary>
    /// Contains removed records, where <see cref="IRecord{T}.Time"/> is the time of this record. Rest of the properties should also relate to this removed record.
    /// In limited range leaderboards, this will often connect with <see cref="NewRecords"/>.
    /// </summary>
    public IEnumerable<IRecord<TPlayerId>> RemovedRecords { get; init; }
    /// <summary>
    /// Contains records that became worse, where <see cref="IRecord{T}.Time"/> is the time of the previous (better) record. Rest of the properties should also relate to the previous (better) record.
    /// To get the worse record, try fetching either newer change by the player, or if none are found, use the current leaderboard data.
    /// </summary>
    public IEnumerable<IRecord<TPlayerId>> WorsenRecords { get; init; }
    /// <summary>
    /// Contains records that were pushed off the leaderboard, where <see cref="IRecord{T}.Time"/> is the time of this record. Rest of the properties should also relate to this pushed off record.
    /// This enumerable is set only in limited range leaderboards and will always relate with <see cref="NewRecords"/>.
    /// </summary>
    public IEnumerable<IRecord<TPlayerId>> PushedOffRecords { get; init; }

    public Top10Changes(
        IEnumerable<IRecord<TPlayerId>> newRecords,
        IEnumerable<IRecord<TPlayerId>> improvedRecords,
        IEnumerable<IRecord<TPlayerId>> removedRecords,
        IEnumerable<IRecord<TPlayerId>> worsenRecords,
        IEnumerable<IRecord<TPlayerId>> pushedOffRecords)
    {
        NewRecords = newRecords;
        ImprovedRecords = improvedRecords;
        RemovedRecords = removedRecords;
        WorsenRecords = worsenRecords;
        PushedOffRecords = pushedOffRecords;
    }
}
