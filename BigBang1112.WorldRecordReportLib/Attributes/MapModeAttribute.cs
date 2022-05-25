namespace BigBang1112.WorldRecordReportLib.Attributes;

[AttributeUsage(AttributeTargets.Field)]
public class MapModeAttribute : Attribute
{
    public string Name { get; }

    public MapModeAttribute(string name)
    {
        Name = name;
    }
}
