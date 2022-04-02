using BigBang1112.WorldRecordReportLib.Attributes;
using BigBang1112.WorldRecordReportLib.Data;

namespace BigBang1112.WorldRecordReportLib.Enums;

public enum TmxSite
{
    [TmxSite(NameConsts.TMXSiteNations, "http://nations.tm-exchange.com/")] Nations = 1,
    [TmxSite(NameConsts.TMXSiteUnited, "https://united.tm-exchange.com/")] United = 2,
    [TmxSite(NameConsts.TMXSiteTMNF, "https://tmnforever.tm-exchange.com/")] TMNF = 3,
    [TmxSite(NameConsts.TMXSiteTM2, "https://tm.mania-exchange.com/")] TM2 = 4,
    [TmxSite(NameConsts.TMXSiteTrackmania, "https://trackmania.exchange/")] Trackmania = 5
}
