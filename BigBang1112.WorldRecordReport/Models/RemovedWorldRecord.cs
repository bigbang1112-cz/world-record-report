using BigBang1112.WorldRecordReport.Models.Db;

namespace BigBang1112.WorldRecordReport.Models;

public record RemovedWorldRecord(WorldRecordModel Previous, WorldRecordModel Current);
