namespace BigBang1112.WorldRecordReportLib.Models.ReportScopes;

public record ReportScopeTM2Official : ReportScope
{
    public ReportScopeChanges? Changes { get; init; }
    public ReportScopeWorldRecord? WR { get; init; }
}
