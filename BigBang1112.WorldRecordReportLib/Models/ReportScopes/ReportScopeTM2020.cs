using BigBang1112.WorldRecordReportLib.Attributes.ReportScope;

namespace BigBang1112.WorldRecordReportLib.Models.ReportScopes;

public record ReportScopeTM2020 : ReportScope
{
    [ReportScopeExplanation("Reports every tracked event happening in the current campaign of TM2020")]
    public ReportScopeTM2020CurrentCampaign? CurrentCampaign { get; init; }
    
    [ReportScopeExplanation("Reports every tracked event happening in the previous campaigns of TM2020")]
    public ReportScopeTM2020PreviousCampaigns? PreviousCampaigns { get; init; }

    [ReportScopeExplanation("Reports every tracked event happening on the training maps of TM2020")]
    public ReportScopeTM2020TrainingMaps? TrainingMaps { get; init; }
}
