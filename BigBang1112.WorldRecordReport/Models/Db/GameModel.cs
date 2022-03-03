using System.ComponentModel.DataAnnotations;

namespace BigBang1112.WorldRecordReport.Models.Db;

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
}
