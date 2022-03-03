using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BigBang1112.WorldRecordReport.Models.Db;

public class TmxLoginModel
{
    public int Id { get; set; }

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
}