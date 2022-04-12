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

    private static string GetStandardOfficialLeaderboardPath(Game game, string mapUid, string zone, string scoreContext)
    {
        var gameFolder = GetOfficialLeaderboardGameFolder(game);

        AssignScoreContextSuffixIfNotEmpty(ref scoreContext);

        return $"records/{gameFolder}/{zone}/{mapUid}{scoreContext}";
    }

    public bool OfficialLeaderboardExists(Game game, string mapUid, string zone = "World", string scoreContext = "")
    {
        var path = GetStandardOfficialLeaderboardPath(game, mapUid, zone, scoreContext);

        return _fileHostService.JsonExistsInApi(ApiVersion, path);
    }

    private static void AssignScoreContextSuffixIfNotEmpty(ref string scoreContext)
    {
        if (!string.IsNullOrEmpty(scoreContext))
        {
            scoreContext = $"-{scoreContext}";
        }
    }

    public void SaveTM2020Leaderboard(IEnumerable<TM2020Record> records, string mapUid, string zone = "World", string scoreContext = "")
    {
        var path = GetStandardOfficialLeaderboardPath(Game.TM2020, mapUid, zone, scoreContext);

        _fileHostService.SaveToApi(records, ApiVersion, path);
    }

    public async Task SaveTM2020LeaderboardAsync(IEnumerable<TM2020Record> records, string mapUid, string zone = "World", string scoreContext = "", CancellationToken cancellationToken = default)
    {
        var path = GetStandardOfficialLeaderboardPath(Game.TM2020, mapUid, zone, scoreContext);

        await _fileHostService.SaveToApiAsync(records, ApiVersion, path, cancellationToken);
    }

    public async Task<ReadOnlyCollection<TM2020Record>> GetTM2020LeaderboardAsync(string mapUid, string zone = "World", string scoreContext = "", CancellationToken cancellationToken = default)
    {
        var path = GetStandardOfficialLeaderboardPath(Game.TM2020, mapUid, zone, scoreContext);

        var records = await _fileHostService.GetFromApiAsync<TM2020Record[]>(ApiVersion, path, cancellationToken);

        return new ReadOnlyCollection<TM2020Record>(records);
    }

    private static string GetOfficialLeaderboardGameFolder(Game game) => game switch
    {
        Game.TM2 => "tm2",
        Game.TMUF => "tmuf",
        Game.TM2020 => "tm2020",
        _ => throw new NotSupportedException(),
    };
}
