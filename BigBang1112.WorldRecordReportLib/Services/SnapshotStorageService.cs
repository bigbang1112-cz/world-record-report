using BigBang1112.Services;
using BigBang1112.WorldRecordReportLib.Enums;
using BigBang1112.WorldRecordReportLib.Models;

namespace BigBang1112.WorldRecordReportLib.Services;

public class SnapshotStorageService
{
    private const int ApiVersion = 1;

    private readonly IFileHostService _fileHostService;

    public SnapshotStorageService(IFileHostService fileHostService)
    {
        _fileHostService = fileHostService;
    }

    // wtf is Standard?
    private static string GetStandardOfficialLeaderboardSnapshotPath(Game game, DateTimeOffset date, string mapUid, string zone, string scoreContext)
    {
        var gameFolder = GetOfficialLeaderboardGameFolder(game);

        AssignScoreContextSuffixIfNotEmpty(ref scoreContext);

        return $"snapshots/{gameFolder}/{zone}/{date.ToUnixTimeSeconds()}_{mapUid}{scoreContext}";
    }

    public void SaveTM2020Leaderboard(IEnumerable<TM2020Record> records,
                                      DateTimeOffset date,
                                      string mapUid,
                                      string zone = "World",
                                      string scoreContext = "")
    {
        var path = GetStandardOfficialLeaderboardSnapshotPath(Game.TM2020, date, mapUid, zone, scoreContext);

        _fileHostService.SaveToApi(records, ApiVersion, path);
    }

    public async Task SaveTM2020LeaderboardAsync(IEnumerable<TM2020Record> records,
                                                 DateTimeOffset date,
                                                 string mapUid,
                                                 string zone = "World",
                                                 string scoreContext = "",
                                                 CancellationToken cancellationToken = default)
    {
        var path = GetStandardOfficialLeaderboardSnapshotPath(Game.TM2020, date, mapUid, zone, scoreContext);

        await _fileHostService.SaveToApiAsync(records, ApiVersion, path, cancellationToken);
    }

    public async Task SaveTM2LeaderboardAsync(IEnumerable<TM2Record> records,
                                              DateTimeOffset date,
                                              string mapUid,
                                              string zone = "World",
                                              string scoreContext = "",
                                              CancellationToken cancellationToken = default)
    {
        var path = GetStandardOfficialLeaderboardSnapshotPath(Game.TM2, date, mapUid, zone, scoreContext);
        
        await _fileHostService.SaveToApiAsync(records, ApiVersion, path, cancellationToken);
    }

    private static void AssignScoreContextSuffixIfNotEmpty(ref string scoreContext)
    {
        if (!string.IsNullOrEmpty(scoreContext))
        {
            scoreContext = $"-{scoreContext}";
        }
    }

    private static string GetOfficialLeaderboardGameFolder(Game game) => game switch
    {
        Game.TM2 => "tm2",
        Game.TMUF => "tmuf",
        Game.TM2020 => "tm2020",
        _ => throw new NotSupportedException(),
    };
}
