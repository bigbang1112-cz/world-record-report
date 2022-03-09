using BigBang1112.WorldRecordReportLib.Data;
using System.ComponentModel.DataAnnotations;

namespace BigBang1112.WorldRecordReportLib.Models.Db;

public class GameModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(255)]
    public string Name { get; set; } = default!;

    [StringLength(255)]
    public string? DisplayName { get; set; }

    public virtual ICollection<LoginModel> Logins { get; set; } = default!;
    public virtual ICollection<MapModel> Maps { get; set; } = default!;

    public override string ToString()
    {
        return string.IsNullOrWhiteSpace(DisplayName) ? Name : DisplayName;
    }

    public bool IsTMUF()
    {
        return Name == NameConsts.GameTMUFName;
    }

    public bool IsTM2()
    {
        return Name == NameConsts.GameTM2Name;
    }
}
