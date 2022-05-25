using System.Collections.ObjectModel;
using BigBang1112.WorldRecordReportLib.Models;
using Mapster;

namespace BigBang1112.WorldRecordReportLib.Services;

public class RefreshScheduleService
{
    public ReadOnlyCollection<MapGroupModel>? TM2OfficialMapGroupCycle { get; private set; }
    public int? CurrentTM2OfficialMapGroup { get; private set; }
    public ReadOnlyCollection<MapModel>? TM2020CurrentCampaignMapCycle { get; private set; }
    public int? CurrentTM2020CurrentCampaignMap { get; private set; }
    public ReadOnlyCollection<MapModel>? TM2020PreviousCampaignsMapCycle { get; private set; }
    public int? CurrentTM2020PreviousCampaignsMap { get; private set; }
    public ReadOnlyCollection<MapModel>? TM2020TrainingMapCycle { get; private set; }
    public int? CurrentTM2020TrainingMap { get; private set; }

    public void SetupTM2Official(IEnumerable<MapGroupModel> mapGroups)
    {
        TM2OfficialMapGroupCycle = new((mapGroups as IList<MapGroupModel>) ?? mapGroups.ToList());
    }

    public void SetupTM2020CurrentCampaign(IEnumerable<MapModel> maps)
    {
        TM2020CurrentCampaignMapCycle = new((maps as IList<MapModel>) ?? maps.ToList());
    }

    public void SetupTM2020PreviousCampaigns(IEnumerable<MapModel> maps)
    {
        TM2020PreviousCampaignsMapCycle = new((maps as IList<MapModel>) ?? maps.ToList());
    }

    public void SetupTM2020TrainingMaps(IEnumerable<MapModel> maps)
    {
        TM2020TrainingMapCycle = new((maps as IList<MapModel>) ?? maps.ToList());
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

    public MapModel? NextTM2020CurrentCampaignMap()
    {
        if (TM2020CurrentCampaignMapCycle is null || TM2020CurrentCampaignMapCycle.Count == 0)
        {
            return null;
        }

        CurrentTM2020CurrentCampaignMap = (CurrentTM2020CurrentCampaignMap + 1) % TM2020CurrentCampaignMapCycle.Count ?? 0;

        return TM2020CurrentCampaignMapCycle[CurrentTM2020CurrentCampaignMap.Value];
    }

    public MapModel? NextTM2020PreviousCampaignMap()
    {
        if (TM2020PreviousCampaignsMapCycle is null || TM2020PreviousCampaignsMapCycle.Count == 0)
        {
            return null;
        }

        CurrentTM2020PreviousCampaignsMap = (CurrentTM2020PreviousCampaignsMap + 1) % TM2020PreviousCampaignsMapCycle.Count ?? 0;

        return TM2020PreviousCampaignsMapCycle[CurrentTM2020PreviousCampaignsMap.Value];
    }

    public MapModel? NextTM2020TrainingMap()
    {
        if (TM2020TrainingMapCycle is null || TM2020TrainingMapCycle.Count == 0)
        {
            return null;
        }

        CurrentTM2020TrainingMap = (CurrentTM2020TrainingMap + 1) % TM2020TrainingMapCycle.Count ?? 0;

        return TM2020TrainingMapCycle[CurrentTM2020TrainingMap.Value];
    }
}
