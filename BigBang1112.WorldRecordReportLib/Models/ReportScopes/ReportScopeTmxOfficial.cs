namespace BigBang1112.WorldRecordReportLib.Models.ReportScopes;

public record ReportScopeTmxOfficial : ReportScope
{
    public ReportScopeTmxSearchFilter? Changes { get; init; }
    public ReportScopeTmxSearchFilter? WR { get; init; }
}
