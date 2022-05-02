using BigBang1112.WorldRecordReportLib.Attributes.ReportScope;

namespace BigBang1112.WorldRecordReportLib.Models.ReportScopes;

[ReportScopeParam("Nadeo TMUF")]
[ReportScopeParam("Nadeo TMNF")]
[ReportScopeParam("StarTrack")]
[ReportScopeParam("Classic")]
public record ReportScopeTmxSearchFilter : ReportScopeWithParam
{
}
