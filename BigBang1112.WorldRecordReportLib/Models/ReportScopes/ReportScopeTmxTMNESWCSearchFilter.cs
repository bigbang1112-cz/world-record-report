using BigBang1112.WorldRecordReportLib.Attributes.ReportScope;

namespace BigBang1112.WorldRecordReportLib.Models.ReportScopes;

[ReportScopeParam("Nadeo")]
[ReportScopeParam("Classic")]
public record ReportScopeTmxTMNESWCSearchFilter : ReportScopeWithParam
{
}
