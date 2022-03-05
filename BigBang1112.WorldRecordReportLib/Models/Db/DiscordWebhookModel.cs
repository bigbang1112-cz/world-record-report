using System.ComponentModel.DataAnnotations;

namespace BigBang1112.WorldRecordReportLib.Models.Db;

public class DiscordWebhookModel
{
    public int Id { get; set; }

    [Required]
    public Guid Guid { get; set; }

    [Required]
    public virtual AssociatedAccountModel Account { get; set; } = default!;

    [StringLength(255)]
    public string? DisplayName { get; set; }

    [Required]
    [Encrypted]
    [StringLength(255)]
    public string Url { get; set; } = default!;

    public bool Disabled { get; set; }

    [StringLength(1024)]
    public string? Filter { get; set; }

    public virtual ICollection<DiscordWebhookMessageModel> Messages { get; set; } = default!;
}
