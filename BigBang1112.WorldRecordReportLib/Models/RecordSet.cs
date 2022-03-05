namespace BigBang1112.WorldRecordReportLib.Models;

public class RecordSet
{
    public IEnumerable<RecordSetDetailedRecord> Records { get; init; }
    public IEnumerable<(int time, int count)> Times { get; init; }

    public RecordSet(IEnumerable<RecordSetDetailedRecord> records, IEnumerable<(int time, int count)> times)
    {
        Records = records;
        Times = times;
    }

    public int GetRecordCount()
    {
        return Times.Where(x => x.time != -1).Sum(x => x.count);
    }
}
