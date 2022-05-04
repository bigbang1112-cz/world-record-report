using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BigBang1112.WorldRecordReportLib.Models.Db;

public class TmxLoginModel : DbModel
{
    [Required]
    public int UserId { get; set; }

    [MaxLength(255)]
    public string? Nickname { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? JoinedOn { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime LastSeenOn { get; set; }

    [Required]
    public virtual TmxSiteModel Site { get; set; } = default!;

    public string GetMdLink()
    {
        return $"[{Nickname?.EscapeDiscord() ?? UserId.ToString()}]({GetInfoUrl()})";
    }

    public string GetInfoUrl()
    {
        return $"{Site.Url}usershow/{UserId}";
    }
}
