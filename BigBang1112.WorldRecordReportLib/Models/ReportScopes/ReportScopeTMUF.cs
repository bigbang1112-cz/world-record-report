using BigBang1112.WorldRecordReportLib.Attributes.ReportScope;

namespace BigBang1112.WorldRecordReportLib.Models.ReportScopes;

public record ReportScopeTMUF : ReportScope
{
    [ReportScopeExplanation("Reports every tracked event happening on both TMUF and TMNF TMX")]
    public ReportScopeTmx? TMX { get; init; }
}
