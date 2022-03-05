using System.ComponentModel.DataAnnotations;

namespace BigBang1112.WorldRecordReportLib.Models.Db;

public class RefreshModel
{
    public int Id { get; set; }

    [Required]
    public Guid Guid { get; set; }

    [Required] // Millisecond accurate refresh time is useful
    public DateTime OccurredOn { get; set; }

    public virtual RefreshLoopModel? RefreshLoop { get; set; }

    public virtual RefreshModel? NextRefresh { get; set; }
    public int? NextRefreshId { get; set; }

    public virtual MapGroupModel? MapGroup { get; set; }

    public virtual TmxLoginModel? TmxLogin { get; set; }

    public bool Ready { get; set; }

    public override string ToString()
    {
        if (MapGroup is not null)
            return $"Refresh of {MapGroup.DisplayName ?? MapGroup.Guid.ToString()} from {MapGroup.TitlePack}";
        return $"{OccurredOn}";
    }
}
