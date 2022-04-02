using BigBang1112.Models.Db;
using System.ComponentModel.DataAnnotations;

namespace BigBang1112.WorldRecordReportLib.Models.Db;

public class CampaignModel : DbModel
{
    [Required]
    public virtual GameModel Game { get; set; } = default!;

    [StringLength(255)]
    public string? Name { get; set; }

    [StringLength(255)]
    public string? LeaderboardUid { get; set; }
}
