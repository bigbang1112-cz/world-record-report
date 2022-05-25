namespace BigBang1112.WorldRecordReportLib.Models;

public class MapRefreshData
{
    public string MapUid { get; init; } = "";
    public string Name { get; init; } = "";
    public string DeformattedName { get; init; } = "";

    public override string ToString()
    {
        return $"{DeformattedName} ({MapUid})";
    }
}
