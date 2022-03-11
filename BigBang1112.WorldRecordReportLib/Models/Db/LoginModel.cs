using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TmEssentials;

namespace BigBang1112.WorldRecordReportLib.Models.Db;

public class LoginModel
{
    public int Id { get; set; }

    [Required]
    public virtual GameModel Game { get; set; } = default!;

    [Required]
    [StringLength(255)]
    public string Name { get; set; } = default!;

    [StringLength(255)]
    public string? Nickname { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? JoinedOn { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime LastSeenOn { get; set; }

    public override string ToString()
    {
        return string.IsNullOrWhiteSpace(Nickname) ? Name : Nickname;
    }

    public string GetDeformattedNickname()
    {
        return string.IsNullOrWhiteSpace(Nickname) ? Name : TextFormatter.Deformat(Nickname).Trim();
    }
}
