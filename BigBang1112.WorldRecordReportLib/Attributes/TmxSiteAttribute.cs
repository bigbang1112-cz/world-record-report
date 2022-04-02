namespace BigBang1112.WorldRecordReportLib.Attributes;

[AttributeUsage(AttributeTargets.Field)]
public class TmxSiteAttribute : Attribute
{
    public string ShortName { get; }
    public string Url { get; }

    public TmxSiteAttribute(string shortName, string url)
    {
        ShortName = shortName;
        Url = url;
    }
}
