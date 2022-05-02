namespace BigBang1112.WorldRecordReportLib.Models.ReportScopes;

public record ReportScopeTM2Nadeo : ReportScope
{
    public ReportScopeTM2NadeoTitlePack? Changes { get; init; }
    public ReportScopeTM2NadeoTitlePack? WR { get; init; }
}
