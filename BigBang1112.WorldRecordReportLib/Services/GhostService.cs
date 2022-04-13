using BigBang1112.Services;
using BigBang1112.WorldRecordReportLib.Models;
using Microsoft.Extensions.Logging;
using TmEssentials;

namespace BigBang1112.WorldRecordReportLib.Services;

public class GhostService : IGhostService
{
    private readonly IFileHostService _fileHostService;
    private readonly HttpClient _http;
    private readonly ILogger<GhostService> _logger;

    public GhostService(IFileHostService fileHostService, HttpClient http, ILogger<GhostService> logger)
    {
        _fileHostService = fileHostService;
        _http = http;
        _logger = logger;
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
        _logger.LogInformation("Downloading {time} on {mapUid} by {login}...", time, mapUid, login);

        using var response = await _http.GetAsync(replayUrl);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Ghost not downloaded (status code: {code}). Using current time instead.", response.StatusCode);

            // access denied or not found
            return DateTimeOffset.UtcNow;
        }

        using var fileStream = File.Create(GetGhostFullPath(mapUid, time, login));
        using var ghostStream = await response.Content.ReadAsStreamAsync();

        _logger.LogInformation("Downloaded.");

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
