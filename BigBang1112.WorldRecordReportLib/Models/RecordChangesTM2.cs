namespace BigBang1112.WorldRecordReportLib.Models;

public record RecordChangesTM2(WorldRecordChangeTM2? WorldRecordChange,
                               RecordCountModel? NewRecordCount,
                               IEnumerable<RecordSetDetailedChangeModel>? RecordChanges,
                               LeaderboardChangesRich<string>? LeaderboardChanges);
