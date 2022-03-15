using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BigBang1112.WorldRecordReportLib.Models.Db;

public class RecordSetDetailedChangeModel
{
    public int Id { get; set; }

    [Required]
    public virtual MapModel Map { get; set; } = default!;

    public RecordSetDetailedChangeType Type { get; set; } // New, Improvement, Removed, Worsen, PushedOff

    [Required]
    public virtual LoginModel Login { get; set; } = default!;

    public int? Rank { get; set; }

    public int? Time { get; set; }

    [StringLength(255)]
    public string? ReplayUrl { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? DrivenBefore { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? DrivenOn { get; set; }
}
