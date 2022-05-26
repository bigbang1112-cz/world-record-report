using BigBang1112.WorldRecordReportLib.Models;
using TmEssentials;
using TmXmlRpc;

namespace BigBang1112.WorldRecordReportLib.Services;

public interface IGhostService
{
    Task<DateTimeOffset?> DownloadGhostAndGetTimestampAsync(string mapUid, MapLeaderBoardPlayer record);
    Task<DateTimeOffset?> DownloadGhostAndGetTimestampAsync(string mapUid, TM2Record record);
    Task<DateTimeOffset?> DownloadGhostAndGetTimestampAsync(string mapUid, string replayUrl, TimeInt32 time, string login);
    string GetGhostFileName(string mapUid, TimeInt32 time, string login);
    string GetGhostFullPath(string mapUid, TimeInt32 time, string login);
    string GetGhostFullPath(string mapUid, int timeInMilliseconds, string login);
    string GetGhostFileName(string mapUid, int timeInMilliseconds, string login);
    bool GhostExists(string mapUid, TM2Record record);
    bool GhostExists(string mapUid, TimeInt32 time, string login);
    Stream? GetGhostStream(string mapUid, int timeInMilliseconds, string login);
    DateTimeOffset? GetGhostLastModified(string mapUid, int timeInMilliseconds, string login);
    bool GhostExists(string mapUid, int timeInMilliseconds, string login);
    Task<DateTimeOffset?> DownloadGhostTimestampAsync(string replayUrl);
}
