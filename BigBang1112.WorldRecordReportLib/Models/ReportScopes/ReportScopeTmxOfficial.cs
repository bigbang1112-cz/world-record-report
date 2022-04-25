namespace BigBang1112.WorldRecordReportLib.Models.ReportScopes;

public record ReportScopeTmxOfficial : ReportScope
{
    public ReportScopeChanges? Changes { get; init; }
    public ReportScopeWorldRecord? WR { get; init; }
}
