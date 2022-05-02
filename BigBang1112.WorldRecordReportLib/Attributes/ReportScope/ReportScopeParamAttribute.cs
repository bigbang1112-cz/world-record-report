namespace BigBang1112.WorldRecordReportLib.Attributes.ReportScope;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ReportScopeParamAttribute : Attribute
{
    public string Value { get; }

    public ReportScopeParamAttribute(string value)
    {
        Value = value;
    }
}
