using BigBang1112.WorldRecordReportLib.Attributes.ReportScope;

namespace BigBang1112.WorldRecordReportLib.Models.ReportScopes;

public record ReportScopeTmxTMNESWCOfficial : ReportScope
{
    [ReportScopeExplanation("Reports Top 10 leaderboard changes on TMNESWC TMX under an official filter")]
    public ReportScopeTmxTMNESWCSearchFilter? Changes { get; init; }
    
    [ReportScopeExplanation("Reports world records on TMNESWC TMX under an official filter")]
    public ReportScopeTmxTMNESWCSearchFilter? WR { get; init; }
}
