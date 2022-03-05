using System.ComponentModel.DataAnnotations;

namespace BigBang1112.WorldRecordReportLib.Models.Db;

public class EnvModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(255)]
    public string Name { get; set; } = default!;

    [StringLength(255)]
    public string? Name2 { get; set; }

    [StringLength(255)]
    public string? DisplayName { get; set; }

    [MinLength(3)]
    [MaxLength(3)]
    public byte[] Color { get; set; } = default!;

    public virtual ICollection<MapModel> Maps { get; set; } = default!;

    public override string ToString()
    {
        if (DisplayName is null)
            return Name;
        return DisplayName;
    }
}
