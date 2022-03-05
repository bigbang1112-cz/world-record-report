namespace BigBang1112.WorldRecordReportLib.Models;

public class RecordSetChanges
{
    public Dictionary<int, int> NewRecords { get; init; }
    public Dictionary<int, int> RemovedRecords { get; init; }

    public RecordSetChanges(Dictionary<int, int> newRecords, Dictionary<int, int> removedRecords)
    {
        NewRecords = newRecords;
        RemovedRecords = removedRecords;
    }
}
