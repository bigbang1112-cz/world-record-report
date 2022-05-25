using BigBang1112.WorldRecordReportLib.Enums;

namespace BigBang1112.WorldRecordReportLib.Repos;

public class MapModeRepo : EnumRepo<MapModeModel, MapMode>, IMapModeRepo
{
    public MapModeRepo(WrContext context) : base(context)
    {

    }
}
