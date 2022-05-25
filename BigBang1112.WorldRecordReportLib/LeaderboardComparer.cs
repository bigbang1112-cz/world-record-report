using BigBang1112.WorldRecordReportLib.Comparers;
using BigBang1112.WorldRecordReportLib.Models;
using TmEssentials;

namespace BigBang1112.WorldRecordReportLib;

public static class LeaderboardComparer
{
    /// <summary>
    /// Compares two leaderboards and returns their differences.
    /// </summary>
    /// <param name="records">Current leaderboard.</param>
    /// <param name="recordsPrev">Previous leaderboard.</param>
    public static LeaderboardChanges<TPlayerId>? Compare<TPlayerId>(IEnumerable<IRecord<TPlayerId>> records, IEnumerable<IRecord<TPlayerId>> recordsPrev)
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

        return new LeaderboardChanges<TPlayerId>(
            newRecords.ToList(),
            improvedRecords.ToList(),
            removedRecords.ToList(),
            worsenRecords.ToList(),
            pushedOffRecords.ToList());
    }

    public static UniqueRecordChanges? CompareTimes(IEnumerable<UniqueRecord> records, IEnumerable<UniqueRecord> recordsPrev)
    {
        using var newTimesEnumerator = records.GetEnumerator();
        using var prevTimesEnumerator = recordsPrev.GetEnumerator();

        var newTotalRecordCount = records.Sum(x => x.Count);
        var prevTotalRecordCount = recordsPrev.Sum(x => x.Count);

        var mapRecordCountDifference = newTotalRecordCount - prevTotalRecordCount;

        var newRecords = new List<UniqueRecord>();
        var removedRecords = new List<UniqueRecord>();

        var somethingChanged = false;

        while (newTimesEnumerator.MoveNext() && prevTimesEnumerator.MoveNext())
        {
            while (true)
            {
                var (newUniqueTime, newTimeCount) = newTimesEnumerator.Current;
                var (prevUniqueTime, prevTimeCount) = prevTimesEnumerator.Current;

                // Fresh new record/s have appeared
                if (newUniqueTime < prevUniqueTime)
                {
                    somethingChanged = true;

                    var amountOfNewTimes = newTimeCount;

                    newRecords.Add(new UniqueRecord(newUniqueTime, amountOfNewTimes));

                    if (newTimesEnumerator.MoveNext())
                    {
                        continue;
                    }
                }

                // One or more times have been removed and there's no other record with this time
                if (newUniqueTime > prevUniqueTime)
                {
                    somethingChanged = true;

                    var amountOfRemovedTimes = prevTimeCount;

                    if (mapRecordCountDifference < 0)
                    {
                        // Possibly removed

                        removedRecords.Add(new UniqueRecord(prevUniqueTime, amountOfRemovedTimes));
                    }
                    else
                    {
                        // Possibly improvement
                    }

                    if (prevTimesEnumerator.MoveNext())
                    {
                        continue;
                    }
                }

                // New existing time/s
                if (newTimeCount > prevTimeCount)
                {
                    somethingChanged = true;

                    newRecords.Add(new UniqueRecord(newUniqueTime, newTimeCount - prevTimeCount));

                    //newUniqueTime
                }

                // Removed time/s while at least one equal time still exists
                // Or the time has been improved by the same person
                if (newTimeCount < prevTimeCount)
                {
                    somethingChanged = true;

                    //prevUniqueTime

                    if (mapRecordCountDifference < 0)
                    {
                        // The record has been possibly removed

                        removedRecords.Add(new UniqueRecord(prevUniqueTime, prevTimeCount - newTimeCount));
                    }
                    else
                    {
                        // Unpredictable behaviour, better to not investigate
                        // An improvement or removed record while someone has driven a fresh record
                    }
                }

                break;
            }
        }

        if (!somethingChanged)
        {
            return null;
        }

        return new UniqueRecordChanges(newRecords, removedRecords);
    }
}
