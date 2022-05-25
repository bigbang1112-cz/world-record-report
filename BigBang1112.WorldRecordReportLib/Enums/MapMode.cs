using BigBang1112.WorldRecordReportLib.Attributes;
using BigBang1112.WorldRecordReportLib.Data;

namespace BigBang1112.WorldRecordReportLib.Enums;

public enum MapMode
{
    [MapMode(NameConsts.MapModeRace)] Race = 1,
    [MapMode(NameConsts.MapModeStunts)] Stunts = 2
}
