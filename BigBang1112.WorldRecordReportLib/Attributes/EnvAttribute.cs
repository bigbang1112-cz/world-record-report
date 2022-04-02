namespace BigBang1112.WorldRecordReportLib.Attributes;

[AttributeUsage(AttributeTargets.Field)]
public class EnvAttribute : Attribute
{
    public string Name { get; }
    public string? Name2 { get; init; }
    public string? DisplayName { get; init; }
    public byte ColorR { get; init; }
    public byte ColorG { get; init; }
    public byte ColorB { get; init; }

    public EnvAttribute(string name)
    {
        Name = name;
    }
}
