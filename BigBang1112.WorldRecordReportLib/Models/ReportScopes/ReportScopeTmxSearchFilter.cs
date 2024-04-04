using BigBang1112.WorldRecordReportLib.Attributes.ReportScope;

namespace BigBang1112.WorldRecordReportLib.Models.ReportScopes;

[ReportScopeParam("NadeoTMUF", DisplayValue = "Nadeo TMUF")]
[ReportScopeParam("NadeoTMNF", DisplayValue = "Nadeo TMNF")]
[ReportScopeParam("StarTrack")]
[ReportScopeParam("ClassicTMUF", DisplayValue = "Classic TMUF")]
[ReportScopeParam("ClassicTMNF", DisplayValue = "Classic TMNF")]
public record ReportScopeTmxSearchFilter : ReportScopeWithParam
{
}
