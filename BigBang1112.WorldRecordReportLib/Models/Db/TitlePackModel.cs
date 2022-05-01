using System.ComponentModel.DataAnnotations;

namespace BigBang1112.WorldRecordReportLib.Models.Db;

public class TitlePackModel : DbModel
{
    [Required]
    [StringLength(255)]
    public string Name { get; set; } = default!;

    [StringLength(255)]
    public string? DisplayName { get; set; }

    [Required]
    public virtual LoginModel Author { get; set; } = default!;

    public virtual ICollection<MapModel> Maps { get; set; } = default!;
    public virtual ICollection<MapGroupModel> MapGroups { get; set; } = default!;

    public string GetTitleUid()
    {
        return Name + "@" + Author.Name;
    }

    public override string ToString()
    {
        return string.IsNullOrWhiteSpace(DisplayName) ? GetTitleUid() : DisplayName;
    }
}
