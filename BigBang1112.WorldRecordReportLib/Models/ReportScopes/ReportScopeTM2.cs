using BigBang1112.WorldRecordReportLib.Attributes.ReportScope;

namespace BigBang1112.WorldRecordReportLib.Models.ReportScopes;

public record ReportScopeTM2 : ReportScope
{
    [ReportScopeExplanation("Reports every tracked event happening on Nadeo maps in TM2")]
    public ReportScopeTM2Nadeo? Nadeo { get; init; }
}
