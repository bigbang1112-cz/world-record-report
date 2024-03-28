using BigBang1112.WorldRecordReportLib.Attributes.ReportScope;

namespace BigBang1112.WorldRecordReportLib.Models.ReportScopes;

public record ReportScopeTMNESWC : ReportScope
{
    [ReportScopeExplanation("Reports every tracked event happening on TMNESWC TMX")]
    public ReportScopeTmxTMNESWC? TMX { get; init; }
}
