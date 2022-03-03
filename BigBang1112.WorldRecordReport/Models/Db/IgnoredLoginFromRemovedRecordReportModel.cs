using System.ComponentModel.DataAnnotations;

namespace BigBang1112.WorldRecordReport.Models.Db;

public class IgnoredLoginFromRemovedRecordReportModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(255)]
    public string Login { get; set; } = default!;
}
