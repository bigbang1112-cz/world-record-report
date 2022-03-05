using BigBang1112.WorldRecordReportLib.Models.Db;

namespace BigBang1112.WorldRecordReportLib.Models;

public record RemovedWorldRecord(WorldRecordModel Previous, WorldRecordModel Current);
