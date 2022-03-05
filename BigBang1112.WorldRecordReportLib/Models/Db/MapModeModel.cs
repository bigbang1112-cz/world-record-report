using System.ComponentModel.DataAnnotations;

namespace BigBang1112.WorldRecordReportLib.Models.Db;

public class MapModeModel
{
    public int Id { get; set; }

    [StringLength(255)]
    public string Name { get; set; } = default!;
}
