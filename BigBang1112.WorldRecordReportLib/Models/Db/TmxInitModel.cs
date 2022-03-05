using System.ComponentModel.DataAnnotations;

namespace BigBang1112.WorldRecordReportLib.Models.Db;

public class TmxInitModel
{
    public int Id { get; set; }

    [Required]
    public Guid Guid { get; set; }

    [Required]
    public virtual TmxLoginModel? Login { get; set; }

    [Required]
    [StringLength(255)]
    public string Status { get; set; } = default!;
}
