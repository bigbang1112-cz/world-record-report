namespace BigBang1112.WorldRecordReportLib.Models;

public class MapGroupRefreshData
{
    public MapRefreshData[] Maps { get; init; } = Array.Empty<MapRefreshData>();
    public string? TitleId { get; init; }
}
