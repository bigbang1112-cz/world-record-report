namespace BigBang1112.WorldRecordReportLib.Attributes;

[AttributeUsage(AttributeTargets.Field)]
public class GameAttribute : Attribute
{
    public string Name { get; }
    public string? DisplayName { get; init; }

    public GameAttribute(string name)
    {
        Name = name;
    }
}
