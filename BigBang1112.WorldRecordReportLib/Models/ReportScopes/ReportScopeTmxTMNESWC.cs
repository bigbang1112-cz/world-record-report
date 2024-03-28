using BigBang1112.WorldRecordReportLib.Attributes.ReportScope;

namespace BigBang1112.WorldRecordReportLib.Models.ReportScopes;

public record ReportScopeTmxTMNESWC : ReportScope
{
    [ReportScopeExplanation("Reports every tracked event happening on TMNESWC TMX under the official filters (Nadeo, Classic)")]
    public ReportScopeTmxTMNESWCOfficial? Official { get; init; }
}
