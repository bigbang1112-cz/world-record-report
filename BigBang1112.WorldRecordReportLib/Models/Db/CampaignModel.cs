using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BigBang1112.WorldRecordReportLib.Models.Db;

public class CampaignModel : DbModel
{
    [Required]
    public virtual GameModel Game { get; set; } = default!;

    [StringLength(255)]
    public string? Name { get; set; }

    [StringLength(255)]
    public string? LeaderboardUid { get; set; }
    
    public bool IsOver { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime PublishedOn { get; internal set; }
}
