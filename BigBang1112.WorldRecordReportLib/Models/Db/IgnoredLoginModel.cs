using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BigBang1112.WorldRecordReportLib.Models.Db;

public class IgnoredLoginModel
{
    public int Id { get; set; }

    [Required]
    public virtual LoginModel Login { get; set; } = default!;

    [Column(TypeName = "datetime")]
    public DateTime IgnoredOn { get; set; }
}
