using BigBang1112.Services;
using BigBang1112.WorldRecordReportLib.Models;
using TmEssentials;

namespace BigBang1112.WorldRecordReportLib.Services;

public class GhostService : IGhostService
{
    private readonly IFileHostService _fileHostService;
    private readonly HttpClient _http;

    public GhostService(IFileHostService fileHostService, HttpClient http)
    {
        _fileHostService = fileHostService;
        _http = http;
    }

    public bool GhostExists(string mapUid, TimeInt32 time, string login)
    {
        return File.Exists(GetGhostFullPath(mapUid, time, login));
    }

    public bool GhostExists(string mapUid, RecordSetDetailedRecord record)
    {
        return File.Exists(GetGhostFullPath(mapUid, new TimeInt32(record.Time), record.Login));
    }

    public async Task<DateTimeOffset> DownloadGhostAndGetTimestampAsync(string mapUid, LeaderboardRecord record)
    {
        if (record.IsFromManialink)
        {
            return record.Timestamp;
        }

        return await DownloadGhostAndGetTimestampAsync(mapUid, record.ReplayUrl, record.Time, record.Login);
    }

    public async Task<DateTimeOffset> DownloadGhostAndGetTimestampAsync(string mapUid, RecordSetDetailedRecord record)
    {
        if (record.ReplayUrl is null)
        {
            return DateTimeOffset.UtcNow;
        }

        return await DownloadGhostAndGetTimestampAsync(mapUid, record.ReplayUrl, new TimeInt32(record.Time), record.Login);
    }

    public async Task<DateTimeOffset> DownloadGhostAndGetTimestampAsync(string mapUid, string replayUrl, TimeInt32 time, string login)
    {
        using var response = await _http.GetAsync(replayUrl);

        if (!response.IsSuccessStatusCode)
        {
            // access denied or not found
            return DateTimeOffset.UtcNow;
        }

        using var fileStream = File.Create(GetGhostFullPath(mapUid, time, login));
        using var ghostStream = await response.Content.ReadAsStreamAsync();
        await ghostStream.CopyToAsync(fileStream);

        return response.Content.Headers.LastModified.GetValueOrDefault(DateTimeOffset.UtcNow);
    }

    public string GetGhostFullPath(string mapUid, TimeInt32 time, string login)
    {
        return _fileHostService.GetClosedFilePath("Ghosts", GetGhostFileName(mapUid, time, login));
    }

    public string GetGhostFileName(string mapUid, TimeInt32 time, string login)
    {
        return $"{mapUid}_{time.TotalMilliseconds}_{login}.Ghost.Gbx";
    }
}
