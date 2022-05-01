using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BigBang1112.WorldRecordReportLib.Models.Db;

public class DiscordWebhookMessageModel : DbModel
{
    public ulong MessageId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime SentOn { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime ModifiedOn { get; set; }

    public virtual ReportModel? Report { get; set; }

    [Required]
    public virtual DiscordWebhookModel Webhook { get; set; } = default!;

    public bool RemovedOfficially { get; set; }
    public bool RemovedByUser { get; set; }
}
