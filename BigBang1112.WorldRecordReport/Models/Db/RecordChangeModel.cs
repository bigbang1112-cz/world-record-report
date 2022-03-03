namespace BigBang1112.WorldRecordReport.Models.Db;

public class RecordChangeModel
{
    public int Id { get; set; }
    public virtual RecordSetChangeModel RecordSetChange { get; set; } = default!;
    public bool NewRecord { get; set; }
    public int Time { get; set; }
    public short Count { get; set; }
}
