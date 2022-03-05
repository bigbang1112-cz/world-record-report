using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BigBang1112.WorldRecordReportLib.Models.Db;

public class AltReplayModel
{
    public int Id { get; set; }
    [Required] public Guid Guid { get; set; }

    [Required]
    [MaxLength(32)]
    public byte[] Checksum { get; set; } = default!;

    [Column(TypeName = "datetime")]
    public DateTime AddedOn { get; set; }

    [Required]
    public virtual WorldRecordModel WorldRecord { get; set; } = default!;
    public virtual int WorldRecordId { get; set; }
}
