using BigBang1112.WorldRecordReportLib.Attributes.ReportScope;

namespace BigBang1112.WorldRecordReportLib.Models.ReportScopes;

[ReportScopeParam("TMCanyon@nadeo", DisplayValue = NameConsts.TitlePackOfficialCanyonDisplayName)]
[ReportScopeParam("TMStadium@nadeo", DisplayValue = NameConsts.TitlePackOfficialStadiumDisplayName)]
[ReportScopeParam("TMValley@nadeo", DisplayValue = NameConsts.TitlePackOfficialValleyDisplayName)]
[ReportScopeParam("TMLagoon@nadeo", DisplayValue = NameConsts.TitlePackOfficialLagoonDisplayName)]
public record ReportScopeTM2NadeoTitlePack : ReportScopeWithParam
{
}
