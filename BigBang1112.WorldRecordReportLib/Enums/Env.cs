using BigBang1112.WorldRecordReportLib.Attributes;
using BigBang1112.WorldRecordReportLib.Data;

namespace BigBang1112.WorldRecordReportLib.Enums;

public enum Env
{
    [Env("Desert", Name2 = "Speed", ColorR = 255, ColorG = 205, ColorB = 50)] Desert = 1,
    [Env("Snow", Name2 = "Alpine", ColorR = 235, ColorB = 247, ColorG = 246)] Snow = 2,
    [Env("Rally", ColorR = 0, ColorG = 183, ColorB = 107)] Rally = 3,
    
    [Env("Island", ColorR = 62, ColorG = 221, ColorB = 211)] Island = 4,
    [Env("Bay", ColorR = 39, ColorG = 75, ColorB = 206)] Bay = 5,
    [Env("Coast", ColorR = 255, ColorG = 2, ColorB = 141)] Coast = 6,
    
    [Env("Stadium", ColorR = 62, ColorG = 89, ColorB = 119)] Stadium = 7,
    
    [Env("Canyon", ColorR = 239, ColorG = 89, ColorB = 0)] Canyon = 8,
    [Env("Valley", ColorR = 34, ColorG = 247, ColorB = 101)] Valley = 9,
    [Env("Lagoon", ColorR = 38, ColorG = 211, ColorB = 255)] Lagoon = 10,
    
    [Env("Stadium2020", DisplayName = "Stadium 2020", ColorR = 0, ColorG = 191, ColorB = 127)] Stadium2020 = 11
}
