using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BigBang1112.WorldRecordReportLib.Models.Db;

public class NicknameChangeModel
{
    public int Id { get; set; }

    [Required]
    public virtual LoginModel Login { get; set; } = default!;

    [Required]
    [StringLength(255)]
    public string Previous { get; set; } = default!;

    [Required]
    [Column(TypeName = "datetime")]
    public DateTime PreviousLastSeenOn { get; set; }
}
