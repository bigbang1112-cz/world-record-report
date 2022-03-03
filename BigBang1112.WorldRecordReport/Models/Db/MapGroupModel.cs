using System.ComponentModel.DataAnnotations;

namespace BigBang1112.WorldRecordReport.Models.Db;

public class MapGroupModel
{
    public int Id { get; set; }

    [Required]
    public Guid Guid { get; set; }

    public string? DisplayName { get; set; }

    [Required]
    public virtual TitlePackModel TitlePack { get; set; } = default!; // verification needs

    public virtual ICollection<MapModel> Maps { get; set; } = default!;

    public override string ToString()
    {
        return $"{DisplayName ?? Guid.ToString()} from {TitlePack}";
    }
}
