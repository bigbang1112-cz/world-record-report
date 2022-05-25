namespace BigBang1112.WorldRecordReportLib.Models.ReportScopes;

public abstract record ReportScopeWithParam : ReportScope
{
    public string[]? Param { get; set; }
}
