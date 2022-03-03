using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BigBang1112.WorldRecordReport.Models.Db;

public class ReportModel
{
    public enum EType
    {
        NewWorldRecord,
        RemovedWorldRecord
    }

    public int Id { get; set; }

    [Required]
    public Guid Guid { get; set; }

    [Required]
    public virtual EType Type { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime HappenedOn { get; set; }

    public virtual WorldRecordModel WorldRecord { get; set; } = default!;
    public virtual WorldRecordModel? RemovedWorldRecord { get; set; }

    public virtual ICollection<DiscordWebhookMessageModel> DiscordWebhookMessages { get; set; } = default!;
}
