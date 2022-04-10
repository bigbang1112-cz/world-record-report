using BigBang1112.Services;
using BigBang1112.WorldRecordReportLib.Models;
using BigBang1112.WorldRecordReportLib.Models.Db;
using System.IO.Compression;
using System.Text.Json;

namespace BigBang1112.WorldRecordReportLib.Services;

// This should be generalized into RecordStorageService

public class TmxRecordSetService : ITmxRecordSetService
{
    private readonly IFileHostService _fileHostService;

    private static readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public TmxRecordSetService(IFileHostService fileHostService)
    {
        _fileHostService = fileHostService;
    }

    public async Task SaveRecordSetAsync(string fullFileName, TmxReplay[] recordSet, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(fullFileName) ?? throw new Exception());

        using var stream = File.Create(fullFileName);
        using var gzip = new GZipStream(stream, CompressionMode.Compress);
        await JsonSerializer.SerializeAsync(gzip, recordSet, jsonSerializerOptions, cancellationToken);
    }

    public bool RecordSetExists(TmxSiteModel tmxSite, string mapUid)
    {
        return File.Exists(GetFullFilePathOfRecordSet(tmxSite, mapUid));
    }

    public string GetFullFilePathOfRecordSet(TmxSiteModel tmxSite, string mapUid)
    {
        var directoryPath = GetTmxRecordsFolder(tmxSite);
        var filePath = Path.Combine(directoryPath, $"{mapUid}.json.gz");
        return Path.Combine(_fileHostService.GetWebRootPath(), filePath);
    }

    public bool RecordSetExists(TmxSiteModel tmxSite, MapModel map)
    {
        return RecordSetExists(tmxSite, map.MapUid);
    }

    public string GetTmxRecordsFolder(TmxSiteModel tmxSite)
    {
        return Path.Combine(_fileHostService.GetWebRootPath(),
            "api", "v1", "records", $"tmx-{tmxSite.ShortName.ToLower()}", "World");
    }

    public async Task<TmxReplay[]?> GetRecordSetAsync(TmxSiteModel tmxSite, string mapUid, CancellationToken cancellationToken = default)
    {
        var fileName = GetFullFilePathOfRecordSet(tmxSite, mapUid);

        if (!File.Exists(fileName))
        {
            return null;
        }

        using var stream = File.OpenRead(fileName);
        using var gzip = new GZipStream(stream, CompressionMode.Decompress);
        return await JsonHelper.DeserializeAsync<TmxReplay[]>(gzip, jsonSerializerOptions, cancellationToken);
    }
}
