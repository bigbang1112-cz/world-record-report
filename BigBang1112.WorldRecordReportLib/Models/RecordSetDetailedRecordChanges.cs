namespace BigBang1112.WorldRecordReportLib.Models;

public class RecordSetDetailedRecordChanges
{
    public IEnumerable<string> NewRecords { get; init; }
    public IEnumerable<TM2Record> ImprovedRecords { get; init; }
    public IEnumerable<TM2Record> RemovedRecords { get; init; }
    public IEnumerable<TM2Record> WorsenRecords { get; init; }
    public IEnumerable<TM2Record> PushedOffRecords { get; init; }

    public RecordSetDetailedRecordChanges(
        IEnumerable<string> newRecords,
        IEnumerable<TM2Record> improvedRecords,
        IEnumerable<TM2Record> removedRecords,
        IEnumerable<TM2Record> worsenRecords,
        IEnumerable<TM2Record> pushedOffRecords)
    {
        NewRecords = newRecords;
        ImprovedRecords = improvedRecords;
        RemovedRecords = removedRecords;
        WorsenRecords = worsenRecords;
        PushedOffRecords = pushedOffRecords;
    }
}
