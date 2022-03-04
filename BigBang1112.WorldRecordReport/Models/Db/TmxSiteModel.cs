using BigBang1112.WorldRecordReport.Data;
using System.ComponentModel.DataAnnotations;
using TmExchangeApi;

namespace BigBang1112.WorldRecordReport.Models.Db;

public class TmxSiteModel
{
    public int Id { get; set; }

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