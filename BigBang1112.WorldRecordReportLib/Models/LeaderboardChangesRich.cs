namespace BigBang1112.WorldRecordReportLib.Models;

public class LeaderboardChangesRich<TPlayerId> where TPlayerId : notnull
{
    public IEnumerable<IRecord<TPlayerId>> NewRecords { get; init; }
    public IEnumerable<(IRecord<TPlayerId>, IRecord<TPlayerId>)> ImprovedRecords { get; init; }
    public IEnumerable<IRecord<TPlayerId>> RemovedRecords { get; init; }
    public IEnumerable<(IRecord<TPlayerId>, IRecord<TPlayerId>)> WorsenRecords { get; init; }
    public IEnumerable<IRecord<TPlayerId>> PushedOffRecords { get; init; }

    public LeaderboardChangesRich(
        IEnumerable<IRecord<TPlayerId>> newRecords,
        IEnumerable<(IRecord<TPlayerId>, IRecord<TPlayerId>)> improvedRecords,
        IEnumerable<IRecord<TPlayerId>> removedRecords,
        IEnumerable<(IRecord<TPlayerId>, IRecord<TPlayerId>)> worsenRecords,
        IEnumerable<IRecord<TPlayerId>> pushedOffRecords)
    {
        NewRecords = newRecords;
        ImprovedRecords = improvedRecords;
        RemovedRecords = removedRecords;
        WorsenRecords = worsenRecords;
        PushedOffRecords = pushedOffRecords;
    }
}
