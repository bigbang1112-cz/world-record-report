using System.ComponentModel.DataAnnotations;

namespace BigBang1112.WorldRecordReport.Models.Db;

public class TmxSiteModel
{
    public int Id { get; set; }

    [Required]
    public string Url { get; set; } = default!;

    [Required]
    public string ShortName { get; set; } = default!;

    public string? DisplayName { get; set; }
}