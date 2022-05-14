using System.Collections.ObjectModel;
using BigBang1112.Services;
using BigBang1112.WorldRecordReportLib.Enums;
using BigBang1112.WorldRecordReportLib.Models;

namespace BigBang1112.WorldRecordReportLib.Services;

public class RecordStorageService
{
    private const int ApiVersion = 1;

    private readonly IFileHostService _fileHostService;

    public RecordStorageService(IFileHostService fileHostService)
    {
        _fileHostService = fileHostService;
    }

    // wtf is Standard?
    private static string GetStandardOfficialLeaderboardPath(Game game, string mapUid, string zone, string scoreContext)
    {
        var gameFolder = GetOfficialLeaderboardGameFolder(game);

        AssignScoreContextSuffixIfNotEmpty(ref scoreContext);

        return $"records/{gameFolder}/{zone}/{mapUid}{scoreContext}";
    }
    
    private static string GetTmxLeaderboardPath(TmxSite site, string mapUid)
    {
        var gameFolder = GetTmxLeaderboardGameFolder(site);

        return $"records/{gameFolder}/World/{mapUid}";
    }

    public bool OfficialLeaderboardExists(Game game, string mapUid, string zone = "World", string scoreContext = "")
    {
        var path = GetStandardOfficialLeaderboardPath(game, mapUid, zone, scoreContext);

        return _fileHostService.JsonExistsInApi(ApiVersion, path);
    }

    public bool TmxLeaderboardExists(TmxSite site, string mapUid)
    {
        var path = GetTmxLeaderboardPath(site, mapUid);

        return _fileHostService.JsonExistsInApi(ApiVersion, path);
    }

    private static void AssignScoreContextSuffixIfNotEmpty(ref string scoreContext)
    {
        if (!string.IsNullOrEmpty(scoreContext))
        {
            scoreContext = $"-{scoreContext}";
        }
    }

    public void SaveTM2020Leaderboard(IEnumerable<TM2020Record> records,
                                      string mapUid,
                                      string zone = "World",
                                      string scoreContext = "")
    {
        var path = GetStandardOfficialLeaderboardPath(Game.TM2020, mapUid, zone, scoreContext);

        _fileHostService.SaveToApi(records, ApiVersion, path);
    }

    public async Task SaveTM2020LeaderboardAsync(IEnumerable<TM2020Record> records,
                                                 string mapUid,
                                                 string zone = "World",
                                                 string scoreContext = "",
                                                 CancellationToken cancellationToken = default)
    {
        var path = GetStandardOfficialLeaderboardPath(Game.TM2020, mapUid, zone, scoreContext);

        await _fileHostService.SaveToApiAsync(records, ApiVersion, path, cancellationToken);
    }

    public async Task SaveTmxLeaderboardAsync(IEnumerable<TmxReplay> records,
                                              TmxSite tmxSite,
                                              string mapUid,
                                              CancellationToken cancellationToken = default)
    {
        var path = GetTmxLeaderboardPath(tmxSite, mapUid);
        
        await _fileHostService.SaveToApiAsync(records, ApiVersion, path, cancellationToken);
    }

    public async Task SaveTM2LeaderboardAsync(LeaderboardTM2 leaderboard,
                                              string mapUid,
                                              string zone = "World",
                                              string scoreContext = "",
                                              CancellationToken cancellationToken = default)
    {
        var path = GetStandardOfficialLeaderboardPath(Game.TM2, mapUid, zone, scoreContext);
        
        await _fileHostService.SaveToApiAsync(leaderboard, ApiVersion, path, cancellationToken);
    }

    public ReadOnlyCollection<TM2020Record>? GetTM2020Leaderboard(string mapUid, string zone = "World", string scoreContext = "")
    {
        var path = GetStandardOfficialLeaderboardPath(Game.TM2020, mapUid, zone, scoreContext);

        var records = _fileHostService.GetFromApi<TM2020Record[]>(ApiVersion, path);

        if (records is null)
        {
            return null;
        }

        return new ReadOnlyCollection<TM2020Record>(records);
    }

    public async Task<ReadOnlyCollection<TM2020Record>?> GetTM2020LeaderboardAsync(string mapUid,
                                                                                   string zone = "World",
                                                                                   string scoreContext = "",
                                                                                   CancellationToken cancellationToken = default)
    {
        var path = GetStandardOfficialLeaderboardPath(Game.TM2020, mapUid, zone, scoreContext);
        
        return await GetFromApiAsCollectionAsync<TM2020Record>(path, cancellationToken);
    }

    public async Task<LeaderboardTM2?> GetTM2LeaderboardAsync(string mapUid,
                                                              string zone = "World",
                                                              string scoreContext = "",
                                                              CancellationToken cancellationToken = default)
    {
        var path = GetStandardOfficialLeaderboardPath(Game.TM2, mapUid, zone, scoreContext);

        return await _fileHostService.GetFromApiAsync<LeaderboardTM2>(ApiVersion, path, cancellationToken);
    }

    public async Task<ReadOnlyCollection<TmxReplay>?> GetTmxLeaderboardAsync(TmxSite tmxSite,
                                                                             string mapUid,
                                                                             CancellationToken cancellationToken = default)
    {
        var path = GetTmxLeaderboardPath(tmxSite, mapUid);

        return await GetFromApiAsCollectionAsync<TmxReplay>(path, cancellationToken);
    }

    public async Task<IEnumerable<IRecord>?> GetOfficialLeaderboardAsync(Game game,
                                                                         string mapUid,
                                                                         string zone = "World",
                                                                         string scoreContext = "",
                                                                         CancellationToken cancellationToken = default)
    {
        return game switch
        {
            Game.TM2 => (await GetTM2LeaderboardAsync(mapUid, zone, scoreContext, cancellationToken))?.Records,
            Game.TM2020 => await GetTM2020LeaderboardAsync(mapUid, zone, scoreContext, cancellationToken),
            _ => null
        };
    }

    public async Task<IEnumerable<IRecord>?> GetOfficialLeaderboardAsync(MapModel map,
                                                                         string zone = "World",
                                                                         string scoreContext = "",
                                                                         CancellationToken cancellationToken = default)
    {
        return await GetOfficialLeaderboardAsync((Game)map.Game.Id, map.MapUid, zone, scoreContext, cancellationToken);
    }

    public DateTimeOffset? GetOfficialLeaderboardLastUpdatedOn(Game game, string mapUid, string zone = "World", string scoreContext = "")
    {
        var path = GetStandardOfficialLeaderboardPath(game, mapUid, zone, scoreContext);

        return _fileHostService.GetLastModifiedTimeFromApi(ApiVersion, path);
    }

    public DateTimeOffset? GetTM2020LeaderboardLastUpdatedOn(string mapUid, string zone = "World", string scoreContext = "")
    {
        var path = GetStandardOfficialLeaderboardPath(Game.TM2020, mapUid, zone, scoreContext);

        return _fileHostService.GetLastModifiedTimeFromApi(ApiVersion, path);
    }

    private async Task<ReadOnlyCollection<T>?> GetFromApiAsCollectionAsync<T>(string path, CancellationToken cancellationToken)
    {
        var records = await _fileHostService.GetFromApiAsync<T[]>(ApiVersion, path, cancellationToken);

        if (records is null)
        {
            return null;
        }

        return new ReadOnlyCollection<T>(records);
    }

    private static string GetOfficialLeaderboardGameFolder(Game game) => game switch
    {
        Game.TM2 => "tm2",
        Game.TMUF => "tmuf",
        Game.TM2020 => "tm2020",
        _ => throw new NotSupportedException(),
    };

    private static string GetTmxLeaderboardGameFolder(TmxSite site) => site switch
    {
        TmxSite.United => "tmx-united",
        TmxSite.TMNF => "tmx-tmnf",
        _ => throw new NotSupportedException(),
    };
}
