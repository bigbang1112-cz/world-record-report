namespace BigBang1112.WorldRecordReportLib.Models.ReportScopes;

public record ReportScopeTM2020Official : ReportScope
{
    public ReportScopeChanges? Changes { get; init; }
    public ReportScopeWorldRecord? WR { get; init; }
}
