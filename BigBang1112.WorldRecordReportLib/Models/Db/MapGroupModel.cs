using System.ComponentModel.DataAnnotations;

namespace BigBang1112.WorldRecordReportLib.Models.Db;

public class MapGroupModel
{
    public int Id { get; set; }

    [Required]
    public Guid Guid { get; set; }

    public int Number { get; set; }

    public string? DisplayName { get; set; }

    public virtual TitlePackModel? TitlePack { get; set; }

    public virtual GameModel? Game { get; set; }

    public virtual ICollection<MapModel> Maps { get; set; } = default!;

    public override string ToString()
    {
        return $"{DisplayName ?? Guid.ToString()} from {TitlePack}";
    }
}
