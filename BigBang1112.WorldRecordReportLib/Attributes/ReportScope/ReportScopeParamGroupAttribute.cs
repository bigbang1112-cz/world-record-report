namespace BigBang1112.WorldRecordReportLib.Attributes.ReportScope;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ReportScopeParamGroupAttribute : Attribute
{
    public string GroupName { get; }
    public string[] Values { get; }

    public ReportScopeParamGroupAttribute(string groupName, string[] values)
    {
        GroupName = groupName;
        Values = values;
    }
}
