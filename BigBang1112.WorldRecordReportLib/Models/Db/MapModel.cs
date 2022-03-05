using BigBang1112.WorldRecordReportLib.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BigBang1112.WorldRecordReportLib.Models.Db;

public class MapModel
{
    public int Id { get; set; }

    [Required]
    [MaxLength(32)] // 27 chars
    public string MapUid { get; set; } = default!;

    [Required]
    public virtual GameModel Game { get; set; } = default!;

    [Required]
    public virtual EnvModel Environment { get; set; } = default!;

    [Required]
    [StringLength(255)]
    public string Name { get; set; } = default!;

    [StringLength(255)]
    public string? DeformattedName { get; set; }

    public virtual LoginModel Author { get; set; } = default!;

    public virtual TitlePackModel? TitlePack { get; set; }

    public int? MxId { get; set; }

    public Guid? ThumbnailGuid { get; set; } // for TM2020

    public virtual MapGroupModel? Group { get; set; }

    [MinLength(32)]
    [MaxLength(32)]
    public byte[]? Checksum { get; set; }

    [Required]
    [Column(TypeName = "datetime")]
    public DateTime AddedOn { get; set; }

    [Required]
    [Column(TypeName = "datetime")]
    public DateTime UpdatedOn { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? LastActivityOn { get; set; }

    public virtual TmxLoginModel? TmxAuthor { get; set; }

    public virtual MapModeModel? Mode { get; set; }

    public virtual ICollection<WorldRecordModel> WorldRecords { get; set; } = default!;
    public virtual ICollection<RecordSetChangeModel> RecordSetChanges { get; set; } = default!;
    public virtual ICollection<RecordSetDetailedChangeModel> RecordSetDetailedChanges { get; set; } = default!;
    public virtual ICollection<RecordCountModel> RecordCounts { get; set; } = default!;

    public bool IsStuntsMode()
    {
        return Mode?.Name == NameConsts.MapModeStunts;
    }
}
