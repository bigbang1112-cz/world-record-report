using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BigBang1112.WorldRecordReportLib.Models.Db;

public class ReportModel : DbModel
{
    public enum EType
    {
        NewWorldRecord,
        RemovedWorldRecord,
        LeaderboardDifferences
    }

    [Required]
    public Guid Guid { get; set; }

    [Required]
    public virtual EType Type { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime HappenedOn { get; set; }

    public virtual WorldRecordModel? WorldRecord { get; set; }
    public virtual WorldRecordModel? RemovedWorldRecord { get; set; }

    public virtual ICollection<DiscordWebhookMessageModel> DiscordWebhookMessages { get; set; } = default!;
}
