namespace BigBang1112.WorldRecordReportLib.Models;

public class LbManialinkMap
{
    public string MapUid { get; init; } = default!;
    public IEnumerable<LbManialinkMapRecord> Records { get; init; } = default!;
}
