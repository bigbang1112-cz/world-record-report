using System.ComponentModel.DataAnnotations;

namespace BigBang1112.WorldRecordReportLib.Models.Db;

public class AssociatedAccountModel : DbModel
{
    [Required]
    public Guid Guid { get; set; }

    public virtual ICollection<DiscordWebhookModel> DiscordWebhooks { get; set; } = default!;
}
