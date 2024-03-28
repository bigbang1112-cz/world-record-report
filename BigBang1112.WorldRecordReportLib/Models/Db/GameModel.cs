using BigBang1112.Models.Db;
using BigBang1112.WorldRecordReportLib.Data;
using BigBang1112.WorldRecordReportLib.Enums;
using System.ComponentModel.DataAnnotations;

namespace BigBang1112.WorldRecordReportLib.Models.Db;

public class GameModel : DbModel
{
    [Required]
    [StringLength(255)]
    public string Name { get; set; } = default!;

    [StringLength(255)]
    public string? DisplayName { get; set; }

    public virtual ICollection<LoginModel> Logins { get; set; } = default!;

    public override string ToString()
    {
        return string.IsNullOrWhiteSpace(DisplayName) ? Name : DisplayName;
    }

    public bool Is(Game game)
    {
        return Id == (int)game;
    }

    public bool IsTMUF()
    {
        return Is(Game.TMUF);
    }

    public bool IsTM2()
    {
        return Is(Game.TM2);
    }

    public bool IsTM2020()
    {
        return Is(Game.TM2020);
    }

    public bool IsTMN()
    {
        return Is(Game.TMN);
    }
}
