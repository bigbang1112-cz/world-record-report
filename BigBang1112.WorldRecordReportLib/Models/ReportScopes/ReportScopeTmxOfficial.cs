using BigBang1112.WorldRecordReportLib.Attributes.ReportScope;

namespace BigBang1112.WorldRecordReportLib.Models.ReportScopes;

public record ReportScopeTmxOfficial : ReportScope
{
    [ReportScopeExplanation("Reports Top 10 leaderboard changes on TMUF/TMNF TMX under an official filter")]
    public ReportScopeTmxSearchFilter? Changes { get; init; }
    
    [ReportScopeExplanation("Reports world records on TMUF/TMNF TMX under an official filter")]
    public ReportScopeTmxSearchFilter? WR { get; init; }
}
