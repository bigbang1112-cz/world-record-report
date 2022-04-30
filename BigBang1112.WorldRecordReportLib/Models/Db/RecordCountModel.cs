using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BigBang1112.WorldRecordReportLib.Models.Db;

public class RecordCountModel : DbModel
{
    [Required]
    public virtual MapModel Map { get; set; } = default!;

    [Required]
    public int Count { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime Before { get; set; }
}
