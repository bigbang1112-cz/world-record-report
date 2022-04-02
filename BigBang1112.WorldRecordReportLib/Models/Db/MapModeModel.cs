using BigBang1112.Models.Db;
using System.ComponentModel.DataAnnotations;

namespace BigBang1112.WorldRecordReportLib.Models.Db;

public class MapModeModel : DbModel
{
    [StringLength(255)]
    public string Name { get; set; } = default!;
}
