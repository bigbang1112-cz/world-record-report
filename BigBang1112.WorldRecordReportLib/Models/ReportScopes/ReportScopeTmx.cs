using BigBang1112.WorldRecordReportLib.Attributes.ReportScope;

namespace BigBang1112.WorldRecordReportLib.Models.ReportScopes;

public record ReportScopeTmx : ReportScope
{
    [ReportScopeExplanation("Reports every tracked event happening on both TMUF and TMNF TMX under the official filters (Nadeo, Classic, Star)")]
    public ReportScopeTmxOfficial? Official { get; init; }
}
