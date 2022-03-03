namespace BigBang1112.WorldRecordReport.Models;

public class LbManialinkMap
{
    public string MapUid { get; init; } = default!;
    public IEnumerable<LbManialinkMapRecord> Records { get; init; } = default!;
}
