using System.Collections.ObjectModel;
using BigBang1112.WorldRecordReportLib.Models;
using Mapster;

namespace BigBang1112.WorldRecordReportLib.Services;

public class RefreshScheduleService
{
    public ReadOnlyCollection<MapModel>? TM2020OfficialMapCycle { get; private set; }
    public int? CurrentTM2020OfficialMap { get; private set; }
    public ReadOnlyCollection<MapGroupModel>? TM2OfficialMapGroupCycle { get; private set; }
    public int? CurrentTM2OfficialMapGroup { get; private set; }

    public void SetupTM2020Official(IEnumerable<MapModel> maps)
    {
        TM2020OfficialMapCycle = new((maps as IList<MapModel>) ?? maps.ToList());
    }

    public void SetupTM2Official(IEnumerable<MapGroupModel> mapGroups)
    {
        TM2OfficialMapGroupCycle = new((mapGroups as IList<MapGroupModel>) ?? mapGroups.ToList());
    }

    public MapModel? NextTM2020OfficialMap()
    {
        if (TM2020OfficialMapCycle is null || TM2020OfficialMapCycle.Count == 0)
        {
            return null;
        }

        CurrentTM2020OfficialMap = (CurrentTM2020OfficialMap + 1) % TM2020OfficialMapCycle.Count ?? 0;

        return TM2020OfficialMapCycle[CurrentTM2020OfficialMap.Value];
    }

    public MapGroupModel? NextTM2OfficialMapGroup()
    {
        if (TM2OfficialMapGroupCycle is null || TM2OfficialMapGroupCycle.Count == 0)
        {
            return null;
        }

        CurrentTM2OfficialMapGroup = (CurrentTM2OfficialMapGroup + 1) % TM2OfficialMapGroupCycle.Count ?? 0;

        return TM2OfficialMapGroupCycle[CurrentTM2OfficialMapGroup.Value];
    }
}
