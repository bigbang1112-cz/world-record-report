using BigBang1112.WorldRecordReportLib.Attributes.ReportScope;

namespace BigBang1112.WorldRecordReportLib.Models.ReportScopes;

public record ReportScopeTM2020 : ReportScope
{
    [ReportScopeExplanation("Reports every tracked event happening in the current campaign of TM2020")]
    public ReportScopeTM2020Official? Official { get; init; }
}
