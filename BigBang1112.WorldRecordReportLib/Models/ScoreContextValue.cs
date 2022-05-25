namespace BigBang1112.WorldRecordReportLib.Models;

public record ScoreContextValue<T>(T Default, Dictionary<string, T>? Custom = null);
