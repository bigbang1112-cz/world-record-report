using BigBang1112.Services;
using BigBang1112.WorldRecordReportLib.Models;
using Microsoft.Extensions.Logging;
using TmEssentials;
using TmXmlRpc;

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

    public bool GhostExists(string mapUid, int timeInMilliseconds, string login)
    {
        return File.Exists(GetGhostFullPath(mapUid, timeInMilliseconds, login));
    }

    public bool GhostExists(string mapUid, TimeInt32 time, string login)
    {
        return GhostExists(mapUid, time.TotalMilliseconds, login);
    }

    public bool GhostExists(string mapUid, TM2Record record)
    {
        return File.Exists(GetGhostFullPath(mapUid, record.Time, record.Login));
    }

    public async Task<DateTimeOffset?> DownloadGhostAndGetTimestampAsync(string mapUid, MapLeaderBoardPlayer record)
    {
        return await DownloadGhostAndGetTimestampAsync(mapUid, record.ReplayUrl, record.Time, record.Login);
    }

    public async Task<DateTimeOffset?> DownloadGhostAndGetTimestampAsync(string mapUid, TM2Record record)
    {
        if (record.ReplayUrl is null)
        {
            return DateTimeOffset.UtcNow;
        }

        return await DownloadGhostAndGetTimestampAsync(mapUid, record.ReplayUrl, record.Time, record.Login);
    }

    public async Task<DateTimeOffset?> DownloadGhostAndGetTimestampAsync(string mapUid, string replayUrl, TimeInt32 time, string login)
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

        return response.Content.Headers.LastModified;
    }

    public async Task<DateTimeOffset?> DownloadGhostTimestampAsync(string replayUrl)
    {
        _logger.LogInformation("Downloading timestamp of {replayUrl}...", replayUrl);

        using var response = await _http.HeadAsync(replayUrl);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Ghost not downloaded (status code: {code}). Using null instead.", response.StatusCode);

            // access denied or not found
            return null;
        }

        return response.Content.Headers.LastModified;
    }

    public string GetGhostFullPath(string mapUid, int timeInMilliseconds, string login)
    {
        return _fileHostService.GetClosedFilePath("Ghosts", GetGhostFileName(mapUid, timeInMilliseconds, login));
    }

    public string GetGhostFileName(string mapUid, int timeInMilliseconds, string login)
    {
        return $"{mapUid}_{timeInMilliseconds}_{login}.Ghost.Gbx";
    }

    public string GetGhostFullPath(string mapUid, TimeInt32 time, string login)
    {
        return GetGhostFullPath(mapUid, time.TotalMilliseconds, login);
    }

    public string GetGhostFileName(string mapUid, TimeInt32 time, string login)
    {
        return GetGhostFileName(mapUid, time.TotalMilliseconds, login);
    }

    public Stream? GetGhostStream(string mapUid, int timeInMilliseconds, string login)
    {
        if (!GhostExists(mapUid, timeInMilliseconds, login))
        {
            return null;
        }

        return File.OpenRead(GetGhostFullPath(mapUid, timeInMilliseconds, login));
    }

    public DateTimeOffset? GetGhostLastModified(string mapUid, int timeInMilliseconds, string login)
    {
        if (!GhostExists(mapUid, timeInMilliseconds, login))
        {
            return null;
        }

        return File.GetLastWriteTimeUtc(GetGhostFullPath(mapUid, timeInMilliseconds, login));
    }
}
