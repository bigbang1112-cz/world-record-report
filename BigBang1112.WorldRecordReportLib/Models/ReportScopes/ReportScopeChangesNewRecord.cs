namespace BigBang1112.WorldRecordReportLib.Models.ReportScopes;

public record ReportScopeChangesNewRecord : ReportScope
{
    public int Top { get; set; } = 20;
}
