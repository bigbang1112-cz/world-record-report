namespace BigBang1112.WorldRecordReportLib.Attributes.ReportScope;

[AttributeUsage(AttributeTargets.Property)]
public class ReportScopeExplanationAttribute : Attribute
{
    public string Description { get; }

    public ReportScopeExplanationAttribute(string description)
    {
        Description = description;
    }
}
