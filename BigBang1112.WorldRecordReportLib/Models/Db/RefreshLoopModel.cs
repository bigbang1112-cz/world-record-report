using System.ComponentModel.DataAnnotations;

namespace BigBang1112.WorldRecordReportLib.Models.Db;

public class RefreshLoopModel
{
    public int Id { get; set; }

    [Required]
    public Guid Guid { get; set; }

    [StringLength(255)]
    public string? DisplayName { get; set; }

    [Required]
    public virtual RefreshModel StartingRefresh { get; set; } = default!;

    public virtual ICollection<RefreshModel> Refreshes { get; set; } = default!;
}
