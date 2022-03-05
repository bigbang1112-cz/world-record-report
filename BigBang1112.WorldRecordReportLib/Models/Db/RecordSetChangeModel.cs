using System.ComponentModel.DataAnnotations.Schema;

namespace BigBang1112.WorldRecordReportLib.Models.Db;

public class RecordSetChangeModel
{
    public int Id { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime DrivenAfter { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime DrivenBefore { get; set; }

    public virtual MapModel Map { get; set; } = default!;

    public virtual IEnumerable<RecordChangeModel> RecordChanges { get; set; } = default!;
}
