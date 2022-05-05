using BigBang1112.WorldRecordReportLib.Attributes.ReportScope;

namespace BigBang1112.WorldRecordReportLib.Models.ReportScopes;

[ReportScopeParam("NadeoTMUF", DisplayValue = "Nadeo TMUF")]
[ReportScopeParam("NadeoTMNF", DisplayValue = "Nadeo TMNF")]
[ReportScopeParam("StarTrack")]
[ReportScopeParam("Classic")]
public record ReportScopeTmxSearchFilter : ReportScopeWithParam
{
}
