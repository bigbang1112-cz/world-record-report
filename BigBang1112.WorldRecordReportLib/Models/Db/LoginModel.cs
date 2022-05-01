using BigBang1112.Models.Db;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TmEssentials;

namespace BigBang1112.WorldRecordReportLib.Models.Db;

[Index(nameof(Name))]
public class LoginModel : DbModel
{
    [Required]
    public virtual GameModel Game { get; set; } = default!;
    public virtual int GameId { get; set; }

    [Required]
    [StringLength(255)]
    public string Name { get; set; } = default!;

    [StringLength(255)]
    public string? Nickname { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? JoinedOn { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime LastSeenOn { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LastNicknameChangeOn { get; set; }

    public override string ToString()
    {
        return string.IsNullOrWhiteSpace(Nickname) ? Name : Nickname;
    }

    public string GetDeformattedNickname()
    {
        return string.IsNullOrWhiteSpace(Nickname) ? Name : TextFormatter.Deformat(Nickname).Trim();
    }

    public string GetMdLink()
    {
        var escapedNickname = GetDeformattedNickname().EscapeDiscord();

        var infoUrl = GetInfoUrl();

        if (infoUrl is not null)
        {
            return $"[{escapedNickname}]({infoUrl})";
        }

        return escapedNickname;
    }

    public string? GetInfoUrl()
    {
        if (Game.IsTM2020())
        {
            return $"https://trackmania.io/#/player/{Name}";
        }

        return null;
    }
}
