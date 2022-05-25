using BigBang1112.WorldRecordReportLib.Attributes.ReportScope;

namespace BigBang1112.WorldRecordReportLib.Models.ReportScopes;

public record ReportScopeTM2020PreviousCampaigns : ReportScope
{
    [ReportScopeExplanation("Reports Top 10 leaderboard changes in the previous official campaigns of TM2020")]
    public ReportScopeChanges? Changes { get; init; }
    
    [ReportScopeExplanation("Reports world records in the previous official campaigns of TM2020")]
    public ReportScopeWorldRecord? WR { get; init; }
}
