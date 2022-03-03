using System.ComponentModel.DataAnnotations;

namespace BigBang1112.WorldRecordReport.Models.Db;

public class AssociatedAccountModel
{
    public int Id { get; set; }

    [Required]
    public Guid Guid { get; set; }

    public virtual ICollection<DiscordWebhookModel> DiscordWebhooks { get; set; } = default!;
}
