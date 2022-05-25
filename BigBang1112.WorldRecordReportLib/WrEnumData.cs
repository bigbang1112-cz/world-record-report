using BigBang1112.WorldRecordReportLib.Attributes;

namespace BigBang1112.WorldRecordReportLib;

public static class WrEnumData
{
    public static GameModel GameAttributeToModel(GameAttribute att) => new()
    {
        Name = att.Name,
        DisplayName = att.DisplayName
    };
    
    public static EnvModel EnvAttributeToModel(EnvAttribute att) => new()
    {
        Name = att.Name,
        Name2 = att.Name2,
        DisplayName = att.DisplayName,
        Color = new byte[] { att.ColorR, att.ColorG, att.ColorB }
    };

    public static TmxSiteModel TmxSiteAttributeToModel(TmxSiteAttribute att) => new()
    {
        ShortName = att.ShortName,
        Url = att.Url
    };

    public static MapModeModel MapModeAttributeToModel(MapModeAttribute att) => new()
    {
        Name = att.Name
    };
}
