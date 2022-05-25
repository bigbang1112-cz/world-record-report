using BigBang1112.Models.Db;
using BigBang1112.WorldRecordReportLib.Data;
using System.ComponentModel.DataAnnotations;
using ManiaAPI.TMX;

namespace BigBang1112.WorldRecordReportLib.Models.Db;

public class TmxSiteModel : DbModel
{
    [Required]
    public string Url { get; set; } = default!;

    [Required]
    public string ShortName { get; set; } = default!;

    public string? DisplayName { get; set; }

    public TmxSite GetSiteEnum() => ShortName switch
    {
        NameConsts.TMXSiteTMNF => TmxSite.TMNForever,
        NameConsts.TMXSiteUnited => TmxSite.United,
        _ => throw new Exception(),
    };
}
