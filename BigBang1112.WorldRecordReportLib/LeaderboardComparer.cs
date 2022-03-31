using BigBang1112.WorldRecordReportLib.Comparers;
using BigBang1112.WorldRecordReportLib.Models;

namespace BigBang1112.WorldRecordReportLib;

public static class LeaderboardComparer
{
    /// <summary>
    /// Compares two leaderboards and returns their differences.
    /// </summary>
    /// <param name="records">Current leaderboard.</param>
    /// <param name="recordsPrev">Previous leaderboard.</param>
    public static Top10Changes<TPlayerId>? Compare<TPlayerId>(IEnumerable<IRecord<TPlayerId>> records, IEnumerable<IRecord<TPlayerId>> recordsPrev)
        where TPlayerId : notnull
    {
        // Compares equality based on everything except rank
        var changesComparer = new Top10ChangesComparer<TPlayerId>();
        
        // Returns all of the changed records after the leaderboard update
        var currentRecordsWithoutPrevRecords = records.Except(recordsPrev, changesComparer);

        // Returns all of the changed records but as they were before the leaderboard update
        var prevRecordsWithoutCurrentRecords = recordsPrev.Except(records, changesComparer);

        // If there are no changed records
        if (!currentRecordsWithoutPrevRecords.Any() && !prevRecordsWithoutCurrentRecords.Any())
        {
            return null; // somethingChanged is false
        }

        // Compares equality based on the login of the record
        var playerIdComparer = new PlayerIdComparer<TPlayerId>();

        var newCurrentRecords = currentRecordsWithoutPrevRecords.Except(prevRecordsWithoutCurrentRecords, playerIdComparer);
        var removedOrPushedOffRecords = prevRecordsWithoutCurrentRecords.Except(currentRecordsWithoutPrevRecords, playerIdComparer);

        // There's no previous record that can be detected, so only the login is needed
        var newRecords = newCurrentRecords;

        var lastRecord = records.Last();

        // Use the last record value to figure out if the record was actually removed or just pushed off Top 10
        var removedRecords = removedOrPushedOffRecords.Where(record => record.Time < lastRecord.Time);
        var pushedOffRecords = removedOrPushedOffRecords.Where(record => record.Time >= lastRecord.Time);

        // Finds an intersection of logins in the Top 10, resulting in either improvement or (unlikely) worse record
        var previousRecordsThatAreImprovedOrWorsen = prevRecordsWithoutCurrentRecords
            .Intersect(currentRecordsWithoutPrevRecords, playerIdComparer);

        // Compares both records driven by the same login if their time is better than before
        var previousRecordsThatAreImproved = previousRecordsThatAreImprovedOrWorsen
            .WhereWith(currentRecordsWithoutPrevRecords,
                whenMatching: x => x.PlayerId,
                andApplies: (prevRec, currentRec) => currentRec.Time < prevRec.Time);

        // Compares both records driven by the same login if their time is worse than before
        var previousRecordsThatAreWorsen = previousRecordsThatAreImprovedOrWorsen
            .WhereWith(currentRecordsWithoutPrevRecords,
                whenMatching: x => x.PlayerId,
                andApplies: (prevRec, currentRec) => currentRec.Time > prevRec.Time);

        var improvedRecords = previousRecordsThatAreImproved;
        var worsenRecords = previousRecordsThatAreWorsen;

        return new Top10Changes<TPlayerId>(newRecords, improvedRecords, removedRecords, worsenRecords, pushedOffRecords);
    }
}
