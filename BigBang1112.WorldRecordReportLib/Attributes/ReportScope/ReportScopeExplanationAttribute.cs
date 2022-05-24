namespace BigBang1112.WorldRecordReportLib.Attributes.ReportScope;

[AttributeUsage(AttributeTargets.Property)]
public class ReportScopeExplanationAttribute : Attribute
{
    public string Description { get; }
    public string? DisplayName { get; set; }

    public ReportScopeExplanationAttribute(string description)
    {
        Description = description;
    }
}
