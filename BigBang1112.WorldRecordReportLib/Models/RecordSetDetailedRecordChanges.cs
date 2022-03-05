namespace BigBang1112.WorldRecordReportLib.Models;

public class RecordSetDetailedRecordChanges
{
    public IEnumerable<string> NewRecords { get; init; }
    public IEnumerable<RecordSetDetailedRecord> ImprovedRecords { get; init; }
    public IEnumerable<RecordSetDetailedRecord> RemovedRecords { get; init; }
    public IEnumerable<RecordSetDetailedRecord> WorsenRecords { get; init; }
    public IEnumerable<RecordSetDetailedRecord> PushedOffRecords { get; init; }

    public RecordSetDetailedRecordChanges(
        IEnumerable<string> newRecords,
        IEnumerable<RecordSetDetailedRecord> improvedRecords,
        IEnumerable<RecordSetDetailedRecord> removedRecords,
        IEnumerable<RecordSetDetailedRecord> worsenRecords,
        IEnumerable<RecordSetDetailedRecord> pushedOffRecords)
    {
        NewRecords = newRecords;
        ImprovedRecords = improvedRecords;
        RemovedRecords = removedRecords;
        WorsenRecords = worsenRecords;
        PushedOffRecords = pushedOffRecords;
    }
}
