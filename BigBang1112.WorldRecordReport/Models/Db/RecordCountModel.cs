﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BigBang1112.WorldRecordReport.Models.Db;

public class RecordCountModel
{
    public int Id { get; set; }

    [Required]
    public virtual MapModel Map { get; set; } = default!;

    [Required]
    public int Count { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime Before { get; set; }
}
