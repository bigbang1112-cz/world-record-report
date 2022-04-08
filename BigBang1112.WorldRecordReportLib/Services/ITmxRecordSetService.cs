using BigBang1112.WorldRecordReportLib.Models;
using BigBang1112.WorldRecordReportLib.Models.Db;
using Mapster;
using ManiaAPI.TMX;

namespace BigBang1112.WorldRecordReportLib.Services;

public interface ITmxRecordSetService
{
    string GetFullFilePathOfRecordSet(TmxSiteModel tmxSite, string mapUid);

    string GetFullFilePathOfRecordSet(TmxSiteModel tmxSite, MapModel map)
    {
        return GetFullFilePathOfRecordSet(tmxSite, map.MapUid);
    }

    string GetTmxRecordsFolder(TmxSiteModel tmxSite);
    bool RecordSetExists(TmxSiteModel tmxSite, MapModel map);
    bool RecordSetExists(TmxSiteModel tmxSite, string mapUid);
    Task SaveRecordSetAsync(string fullFileName, TmxReplay[] recordSet, CancellationToken cancellationToken = default);

    async Task SaveRecordSetAsync(TmxSiteModel tmxSite, string mapUid, TmxReplay[] recordSet, CancellationToken cancellationToken = default)
    {
        await SaveRecordSetAsync(GetFullFilePathOfRecordSet(tmxSite, mapUid), recordSet, cancellationToken);
    }

    async Task SaveRecordSetAsync(TmxSiteModel tmxSite, string mapUid, ItemCollection<ReplayItem> recordSet, CancellationToken cancellationToken = default)
    {
        await SaveRecordSetAsync(GetFullFilePathOfRecordSet(tmxSite, mapUid), recordSet, cancellationToken);
    }

    async Task SaveRecordSetAsync(TmxSiteModel tmxSite, MapModel map, TmxReplay[] recordSet, CancellationToken cancellationToken = default)
    {
        await SaveRecordSetAsync(tmxSite, map.MapUid, recordSet, cancellationToken);
    }

    async Task SaveRecordSetAsync(TmxSiteModel tmxSite, MapModel map, ItemCollection<ReplayItem> recordSet, CancellationToken cancellationToken = default)
    {
        await SaveRecordSetAsync(tmxSite, map.MapUid, recordSet, cancellationToken);
    }

    async Task SaveRecordSetAsync(string fullFileName, ItemCollection<ReplayItem> recordSet, CancellationToken cancellationToken = default)
    {
        await SaveRecordSetAsync(fullFileName, recordSet.Results.Adapt<TmxReplay[]>(), cancellationToken);
    }

    Task<TmxReplay[]?> GetRecordSetAsync(TmxSiteModel tmxSite, string mapUid, CancellationToken cancellationToken = default);

    async Task<TmxReplay[]?> GetRecordSetAsync(TmxSiteModel tmxSite, MapModel map, CancellationToken cancellationToken = default)
    {
        return await GetRecordSetAsync(tmxSite, map.MapUid, cancellationToken);
    }
}
