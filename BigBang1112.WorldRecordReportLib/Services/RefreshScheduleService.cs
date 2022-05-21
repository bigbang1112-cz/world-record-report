using System.Collections.ObjectModel;
using BigBang1112.WorldRecordReportLib.Models;
using Mapster;

namespace BigBang1112.WorldRecordReportLib.Services;

public class RefreshScheduleService
{
    public ReadOnlyCollection<MapModel>? TM2020OfficialMapCycle { get; private set; }
    public int? CurrentTM2020OfficialMap { get; private set; }
    public ReadOnlyCollection<MapModel>? TM2020OfficialOldMapCycle { get; private set; }
    public int? CurrentTM2020OfficialOldMap { get; private set; }
    public ReadOnlyCollection<MapGroupModel>? TM2OfficialMapGroupCycle { get; private set; }
    public int? CurrentTM2OfficialMapGroup { get; private set; }

    public void SetupTM2020Official(IEnumerable<MapModel> maps)
    {
        TM2020OfficialMapCycle = new((maps as IList<MapModel>) ?? maps.ToList());
    }

    public void SetupTM2020OfficialOld(IEnumerable<MapModel> maps)
    {
        TM2020OfficialOldMapCycle = new((maps as IList<MapModel>) ?? maps.ToList());
    }

    public void SetupTM2Official(IEnumerable<MapGroupModel> mapGroups)
    {
        TM2OfficialMapGroupCycle = new((mapGroups as IList<MapGroupModel>) ?? mapGroups.ToList());
    }

    public MapModel? NextTM2020CurrentCampaignMap()
    {
        if (TM2020OfficialMapCycle is null || TM2020OfficialMapCycle.Count == 0)
        {
            return null;
        }

        CurrentTM2020OfficialMap = (CurrentTM2020OfficialMap + 1) % TM2020OfficialMapCycle.Count ?? 0;

        return TM2020OfficialMapCycle[CurrentTM2020OfficialMap.Value];
    }

    public MapModel? NextTM2020PreviousCampaignMap()
    {
        if (TM2020OfficialOldMapCycle is null || TM2020OfficialOldMapCycle.Count == 0)
        {
            return null;
        }

        CurrentTM2020OfficialOldMap = (CurrentTM2020OfficialOldMap + 1) % TM2020OfficialOldMapCycle.Count ?? 0;

        return TM2020OfficialOldMapCycle[CurrentTM2020OfficialOldMap.Value];
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
