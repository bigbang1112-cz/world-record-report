namespace BigBang1112.WorldRecordReportLib.Models;

public class UniqueRecordChanges
{
    public IEnumerable<UniqueRecord> NewRecords { get; init; }
    public IEnumerable<UniqueRecord> RemovedRecords { get; init; }

    public UniqueRecordChanges(IEnumerable<UniqueRecord> newRecords, IEnumerable<UniqueRecord> removedRecords)
    {
        NewRecords = newRecords;
        RemovedRecords = removedRecords;
    }
}
