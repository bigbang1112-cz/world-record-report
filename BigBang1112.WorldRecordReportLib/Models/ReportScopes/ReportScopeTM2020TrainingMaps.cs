using BigBang1112.WorldRecordReportLib.Attributes.ReportScope;

namespace BigBang1112.WorldRecordReportLib.Models.ReportScopes;

public record ReportScopeTM2020TrainingMaps : ReportScope
{
    [ReportScopeExplanation("Reports Top 10 leaderboard changes on training maps of TM2020")]
    public ReportScopeChanges? Changes { get; init; }
    
    [ReportScopeExplanation("Reports world records on training maps of TM2020")]
    public ReportScopeWorldRecord? WR { get; init; }
}
