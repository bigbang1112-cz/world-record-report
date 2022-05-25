using BigBang1112.WorldRecordReportLib.Models;
using Microsoft.Extensions.Logging;

namespace BigBang1112.WorldRecordReportLib.Services;

public abstract class RefreshService
{
    private readonly ILogger _logger;

    public RefreshService(ILogger logger)
    {
        _logger = logger;
    }

    protected LeaderboardChangesRich<TPlayerId>? CreateLeaderboardChangesRich<TPlayerId>(
        LeaderboardChanges<TPlayerId> diff,
        Dictionary<TPlayerId, IRecord<TPlayerId>> currentRecords,
        Dictionary<TPlayerId, IRecord<TPlayerId>> previousRecords) where TPlayerId : notnull
    {
        if (currentRecords.Count == 0)
        {
            // Can happen if record/s got removed to a point there are none in the leaderboard
            return null;
        }

        var newRecords = new List<IRecord<TPlayerId>>();
        var improvedRecords = new List<(IRecord<TPlayerId>, IRecord<TPlayerId>)>();
        var removedRecords = new List<IRecord<TPlayerId>>();
        var pushedOffRecords = new List<IRecord<TPlayerId>>();
        var worsenedRecords = new List<(IRecord<TPlayerId>, IRecord<TPlayerId>)>();

        foreach (var rec in diff.NewRecords)
        {
            var currentRecord = currentRecords[rec.PlayerId];

            _logger.LogInformation("New record: {rank}) {time} by {player}", currentRecord.Rank, currentRecord.Time, currentRecord.DisplayName);

            newRecords.Add(currentRecord);
        }

        foreach (var rec in diff.ImprovedRecords)
        {
            var currentRecord = currentRecords[rec.PlayerId];
            var prevRecord = previousRecords[rec.PlayerId];

            _logger.LogInformation("Improved record: {previousRank}) {previousTime} to {currentRank}) {currentTime} by {player}",
                prevRecord.Rank, prevRecord.Time, currentRecord.Rank, currentRecord.Time, currentRecord.DisplayName);

            improvedRecords.Add((currentRecord, prevRecord));
        }

        foreach (var rec in diff.RemovedRecords)
        {
            var prevRecord = previousRecords[rec.PlayerId];

            _logger.LogInformation("Removed record: {rank}) {time} by {player}", prevRecord.Rank, prevRecord.Time, prevRecord.DisplayName);

            removedRecords.Add(prevRecord);
        }

        foreach (var rec in diff.PushedOffRecords)
        {
            var prevRecord = previousRecords[rec.PlayerId];

            _logger.LogInformation("Pushed off record: {rank}) {time} by {player}", prevRecord.Rank, prevRecord.Time, prevRecord.DisplayName);

            pushedOffRecords.Add(prevRecord);
        }

        foreach (var rec in diff.WorsenRecords)
        {
            var currentRecord = currentRecords[rec.PlayerId];
            var prevRecord = previousRecords[rec.PlayerId];

            _logger.LogInformation("Worsened record: {previousRank}) {previousTime} to {currentRank}) {currentTime} by {player}",
                prevRecord.Rank, prevRecord.Time, currentRecord.Rank, currentRecord.Time, currentRecord.DisplayName);

            worsenedRecords.Add((currentRecord, prevRecord));
        }

        return new LeaderboardChangesRich<TPlayerId>(newRecords, improvedRecords, removedRecords, worsenedRecords, pushedOffRecords);
    }

    protected IEnumerable<RecordSetDetailedChangeModel> CreateListOfRecordChanges<TPlayerId>(
        LeaderboardChanges<TPlayerId> diff,
        MapModel map,
        Dictionary<TPlayerId, LoginModel> loginModels) where TPlayerId : notnull
    {
        var drivenBefore = DateTime.UtcNow;

        var changes = new List<RecordSetDetailedChangeModel>();

        var allPossibleRecordChanges = new Dictionary<RecordSetDetailedChangeType, IEnumerable<IRecord<TPlayerId>>>
        {
            { RecordSetDetailedChangeType.New, diff.NewRecords },
            { RecordSetDetailedChangeType.Improvement, diff.ImprovedRecords },
            { RecordSetDetailedChangeType.Removed, diff.RemovedRecords },
            { RecordSetDetailedChangeType.Worsen, diff.WorsenRecords },
            { RecordSetDetailedChangeType.PushedOff, diff.PushedOffRecords }
        };

        foreach (var (changeType, recordChanges) in allPossibleRecordChanges)
        {
            foreach (var record in recordChanges)
            {
                var drivenOn = default(DateTime?);

                var recordChange = new RecordSetDetailedChangeModel
                {
                    Login = loginModels[record.PlayerId],
                    Map = map,
                    Type = changeType,
                    DrivenBefore = drivenBefore,
                    DrivenOn = drivenOn
                };

                if (changeType != RecordSetDetailedChangeType.New)
                {
                    recordChange.Rank = record.Rank;
                    recordChange.Time = record.Time.TotalMilliseconds;
                }

                changes.Add(recordChange);
            }
        }

        return changes;
    }

    protected static void UpdateLastRefreshedOn(MapModel mapModel)
    {
        var lastRefreshedOn = DateTime.UtcNow;

        mapModel.LastRefreshedOn = mapModel.LastRefreshedOn is null
            ? new ScoreContextValue<DateTimeOffset>(lastRefreshedOn)
            : (mapModel.LastRefreshedOn with { Default = lastRefreshedOn });
    }
}
