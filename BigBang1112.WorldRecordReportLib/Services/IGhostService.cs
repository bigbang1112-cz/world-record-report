using BigBang1112.WorldRecordReportLib.Models;
using TmEssentials;

namespace BigBang1112.WorldRecordReportLib.Services;

public interface IGhostService
{
    Task<DateTimeOffset> DownloadGhostAndGetTimestampAsync(string mapUid, LeaderboardRecord record);
    Task<DateTimeOffset> DownloadGhostAndGetTimestampAsync(string mapUid, RecordSetDetailedRecord record);
    Task<DateTimeOffset> DownloadGhostAndGetTimestampAsync(string mapUid, string replayUrl, TimeInt32 time, string login);
    string GetGhostFileName(string mapUid, TimeInt32 time, string login);
    string GetGhostFullPath(string mapUid, TimeInt32 time, string login);
    bool GhostExists(string mapUid, RecordSetDetailedRecord record);
}
