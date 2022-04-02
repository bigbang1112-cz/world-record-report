using BigBang1112.WorldRecordReportLib.Attributes;
using BigBang1112.WorldRecordReportLib.Data;

namespace BigBang1112.WorldRecordReportLib.Enums;

public enum Game
{
    [Game(NameConsts.GameTM2Name, DisplayName = NameConsts.GameTM2DisplayName)] TM2 = 1,
    [Game(NameConsts.GameTMUFName, DisplayName = NameConsts.GameTMUFDisplayName)] TMUF = 2,
    [Game(NameConsts.GameTMNFName, DisplayName = NameConsts.GameTMNFDisplayName)] TMNF = 3,
    [Game(NameConsts.GameTMUName, DisplayName = NameConsts.GameTMUDisplayName)] TMU = 4,
    [Game(NameConsts.GameTMSName, DisplayName = NameConsts.GameTMSDisplayName)] TMS = 5,
    [Game(NameConsts.GameTMNName, DisplayName = NameConsts.GameTMNDisplayName)] TMN = 6,
    [Game(NameConsts.GameTMOName, DisplayName = NameConsts.GameTMODisplayName)] TMO = 7,
    [Game(NameConsts.GameTM2020Name, DisplayName = NameConsts.GameTM2020DisplayName)] TM2020 = 8
}
