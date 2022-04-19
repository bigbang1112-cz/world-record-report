namespace BigBang1112.WorldRecordReportLib.Models.ReportScopes;

public record ReportScopeChanges : ReportScope
{
    public ReportScopeChangesNewRecord? NewRecord { get; set; }
}
