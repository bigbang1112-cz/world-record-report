using BigBang1112.WorldRecordReportLib.Attributes.ReportScope;

namespace BigBang1112.WorldRecordReportLib.Models.ReportScopes;

[ReportScopeParam("TMCanyon@nadeo")]
[ReportScopeParam("TMStadium@nadeo")]
[ReportScopeParam("TMValley@nadeo")]
[ReportScopeParam("TMLagoon@nadeo")]
public record ReportScopeTM2NadeoTitlePack : ReportScopeWithParam
{
}
