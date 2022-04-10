using System.Collections.ObjectModel;
using BigBang1112.WorldRecordReportLib.Models;
using Mapster;

namespace BigBang1112.WorldRecordReportLib.Services;

public class RefreshScheduleService
{
    public ReadOnlyCollection<MapRefreshData> TM2020OfficialMapCycle { get; private set; }
    public int? CurrentTM2020OfficialMap { get; private set; }

    public RefreshScheduleService()
    {
        TM2020OfficialMapCycle = new ReadOnlyCollection<MapRefreshData>(Array.Empty<MapRefreshData>());
    }

    public void SetupTM2020Official(IEnumerable<MapModel> maps)
    {
        TM2020OfficialMapCycle = new ReadOnlyCollection<MapRefreshData>(maps.Adapt<IList<MapRefreshData>>());
    }

    public MapRefreshData? NextTM2020OfficialMap()
    {
        if (TM2020OfficialMapCycle.Count == 0)
        {
            return null;
        }

        CurrentTM2020OfficialMap = (CurrentTM2020OfficialMap + 1) % TM2020OfficialMapCycle.Count ?? 0;

        return TM2020OfficialMapCycle[CurrentTM2020OfficialMap.Value];
    }
}
