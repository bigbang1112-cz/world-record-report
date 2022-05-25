using BigBang1112.WorldRecordReportLib.Attributes.ReportScope;

namespace BigBang1112.WorldRecordReportLib.Models.ReportScopes;

public record ReportScopeTM2Nadeo : ReportScope
{
    [ReportScopeExplanation("Reports Top 10 leaderboard changes in Nadeo title packs of TM2")]
    public ReportScopeTM2NadeoTitlePack? Changes { get; init; }

    [ReportScopeExplanation("Reports world records in Nadeo title packs of TM2")]
    public ReportScopeTM2NadeoTitlePack? WR { get; init; }
}
