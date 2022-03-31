using BigBang1112.WorldRecordReportLib.Models;
using Xunit;

namespace BigBang1112.WorldRecordReportLib.Tests.Unit;

public class LeaderboardComparerTests
{
    [Fact]
    public void Compare_NothingChanged()
    {
        var records = Enumerable.Range(1, 10)
            .Select(i => new RecordSetDetailedRecord(
                rank: i,
                login: new string('a', i),
                time: 11111 * i
            ));

        var changes = LeaderboardComparer.Compare(
            records,
            recordsPrev: records);

        Assert.Null(changes);
    }

    [Fact]
    public void Compare_LoginsChanged()
    {
        var recordsPrev = Enumerable.Range(1, 10)
            .Select(i => new RecordSetDetailedRecord(
                rank: i,
                login: new string('a', i), //
                time: 11111 * i
            ));

        var records = Enumerable.Range(1, 10)
            .Select(i => new RecordSetDetailedRecord(
                rank: i,
                login: new string('b', i), //
                time: 11111 * i
            ));

        var changes = LeaderboardComparer.Compare(records, recordsPrev);

        Assert.NotNull(changes);
    }

    [Fact]
    public void Compare_TimesChanged()
    {
        var recordsPrev = Enumerable.Range(1, 10)
            .Select(i => new RecordSetDetailedRecord(
                rank: i,
                login: new string('a', i),
                time: 11111 * i //
            ));

        var records = Enumerable.Range(1, 10)
            .Select(i => new RecordSetDetailedRecord(
                rank: i,
                login: new string('a', i),
                time: 11112 * i //
            ));

        var changes = LeaderboardComparer.Compare(records, recordsPrev);

        Assert.NotNull(changes);
    }

    [Fact]
    public void Compare_NewRecord()
    {
        var recordsPrev = new RecordSetDetailedRecord[]
        {
            new(rank: 1, login: "aaa", time: 10000),
            new(rank: 2, login: "bbb", time: 10001),
            new(rank: 3, login: "ccc", time: 10002),
            new(rank: 4, login: "ddd", time: 10003), //
            new(rank: 5, login: "eee", time: 10005), //
            new(rank: 6, login: "fff", time: 10006),
            new(rank: 7, login: "ggg", time: 10007),
            new(rank: 8, login: "hhh", time: 10008),
            new(rank: 9, login: "iii", time: 10009),
            new(rank: 10, login: "jjj", time: 10010)
        };

        var records = new RecordSetDetailedRecord[]
        {
            new(rank: 1, login: "aaa", time: 10000),
            new(rank: 2, login: "bbb", time: 10001),
            new(rank: 3, login: "ccc", time: 10002),
            new(rank: 4, login: "ddd", time: 10003),
            new(rank: 5, login: "zzz", time: 10004), //
            new(rank: 6, login: "eee", time: 10005),
            new(rank: 7, login: "fff", time: 10006),
            new(rank: 8, login: "ggg", time: 10007),
            new(rank: 9, login: "hhh", time: 10008),
            new(rank: 10, login: "iii", time: 10009)
        };

        var changes = LeaderboardComparer.Compare(records, recordsPrev)!;

        Assert.NotNull(changes);
        Assert.True(changes.NewRecords.Count() == 1);
        Assert.True(changes.PushedOffRecords.Count() == 1);
        Assert.Contains(changes.NewRecords, record =>
             record.PlayerId == "zzz"
          && record.Time == 10004);
        Assert.Contains(changes.PushedOffRecords, record =>
             record.PlayerId == "jjj"
          && record.Time == 10010);
        Assert.Empty(changes.ImprovedRecords);
        Assert.Empty(changes.RemovedRecords);
        Assert.Empty(changes.WorsenRecords);
    }

    [Fact]
    public void Compare_MultipleNewRecords()
    {
        var recordsPrev = new RecordSetDetailedRecord[]
        {
            new(rank: 1, login: "aaa", time: 10000),
            new(rank: 2, login: "bbb", time: 10001), //
            new(rank: 3, login: "ccc", time: 10003), //
            new(rank: 4, login: "ddd", time: 10004), //
            new(rank: 5, login: "eee", time: 10006), //
            new(rank: 6, login: "fff", time: 10008), //
            new(rank: 7, login: "ggg", time: 10009),
            new(rank: 8, login: "hhh", time: 10010), //
            new(rank: 9, login: "iii", time: 10012), //
            new(rank: 10, login: "jjj", time: 10013)
        };

        var records = new RecordSetDetailedRecord[]
        {
            new(rank: 1, login: "aaa", time: 10000),
            new(rank: 2, login: "bbb", time: 10001),
            new(rank: 3, login: "www", time: 10002), //
            new(rank: 4, login: "ccc", time: 10003),
            new(rank: 5, login: "ddd", time: 10004),
            new(rank: 6, login: "xxx", time: 10005), //
            new(rank: 7, login: "eee", time: 10006),
            new(rank: 8, login: "yyy", time: 10007), //
            new(rank: 9, login: "fff", time: 10008),
            new(rank: 10, login: "ggg", time: 10009)
        };

        var changes = LeaderboardComparer.Compare(records, recordsPrev)!;

        Assert.NotNull(changes);
        Assert.True(changes.NewRecords.Count() == 3);
        Assert.True(changes.PushedOffRecords.Count() == 3);
        
        Assert.Contains(changes.NewRecords, record =>
            record.PlayerId == "www"
         && record.Time == 10002);
        Assert.Contains(changes.NewRecords, record =>
            record.PlayerId == "xxx"
         && record.Time == 10005);
        Assert.Contains(changes.NewRecords, record =>
            record.PlayerId == "yyy"
         && record.Time == 10007);

        foreach (var (rank, login, time) in new (int rank, string login, int time)[] {
            (rank: 8, login: "hhh", time: 10010),
            (rank: 9, login: "iii", time: 10012),
            (rank: 10, login: "jjj", time: 10013)
        })
        {
            Assert.Contains(changes.PushedOffRecords, record =>
                record.PlayerId == login
             && record.Time == time);
        }

        Assert.Empty(changes.ImprovedRecords);
        Assert.Empty(changes.RemovedRecords);
        Assert.Empty(changes.WorsenRecords);
    }

    [Fact]
    public void Compare_ImprovedRecord()
    {
        var exampleLogin = "ddd";
        var examplePreviousRank = 4;
        var examplePreviousTime = 10003;

        var recordsPrev = new RecordSetDetailedRecord[]
        {
            new(rank: 1, login: "aaa", time: 10000),
            new(rank: 2, login: "bbb", time: 10001),
            new(rank: 3, login: "ccc", time: 10002),
            new(rank: examplePreviousRank, login: exampleLogin, time: examplePreviousTime),
            new(rank: 5, login: "eee", time: 10004),
            new(rank: 6, login: "fff", time: 10005),
            new(rank: 7, login: "ggg", time: 10006),
            new(rank: 8, login: "hhh", time: 10007),
            new(rank: 9, login: "iii", time: 10008),
            new(rank: 10, login: "jjj", time: 10009)
        };

        var records = new RecordSetDetailedRecord[]
        {
            new(rank: 1, login: exampleLogin, time: 9999),
            new(rank: 2, login: "aaa", time: 10000),
            new(rank: 3, login: "bbb", time: 10001),
            new(rank: 4, login: "ccc", time: 10002),
            new(rank: 5, login: "eee", time: 10004),
            new(rank: 6, login: "fff", time: 10005),
            new(rank: 7, login: "ggg", time: 10006),
            new(rank: 8, login: "hhh", time: 10007),
            new(rank: 9, login: "iii", time: 10008),
            new(rank: 10, login: "jjj", time: 10009)
        };

        var changes = LeaderboardComparer.Compare(records, recordsPrev)!;

        Assert.NotNull(changes);
        Assert.True(changes.ImprovedRecords.Count() == 1);
        Assert.Contains(changes.ImprovedRecords, record =>
            record.PlayerId == exampleLogin
         && record.Time == examplePreviousTime);
        Assert.Empty(changes.NewRecords);
        Assert.Empty(changes.RemovedRecords);
        Assert.Empty(changes.WorsenRecords);
        Assert.Empty(changes.PushedOffRecords);
    }

    [Fact]
    public void Compare_MultipleImprovedRecords()
    {
        var recordsPrev = new RecordSetDetailedRecord[]
        {
            new(rank: 1, login: "aaa", time: 10000),
            new(rank: 2, login: "bbb", time: 10001),
            new(rank: 3, login: "ccc", time: 10002),
            new(rank: 4, login: "ddd", time: 10003),
            new(rank: 5, login: "eee", time: 10004), //
            new(rank: 6, login: "fff", time: 10005),
            new(rank: 7, login: "ggg", time: 10006), //
            new(rank: 8, login: "hhh", time: 10007), //
            new(rank: 9, login: "iii", time: 10008),
            new(rank: 10, login: "jjj", time: 10009) //
        };

        var records = new RecordSetDetailedRecord[]
        {
            new(rank: 1, login: "ggg", time: 9997), //
            new(rank: 2, login: "eee", time: 9998), //
            new(rank: 3, login: "jjj", time: 9999), //
            new(rank: 4, login: "aaa", time: 10000),
            new(rank: 5, login: "bbb", time: 10001),
            new(rank: 6, login: "ccc", time: 10002),
            new(rank: 7, login: "ddd", time: 10003),
            new(rank: 8, login: "hhh", time: 10004), //
            new(rank: 9, login: "fff", time: 10005),
            new(rank: 10, login: "iii", time: 10008)
        };

        var changes = LeaderboardComparer.Compare(records, recordsPrev)!;

        Assert.NotNull(changes);
        Assert.True(changes.ImprovedRecords.Count() == 4);

        // Previous times reminder
        foreach (var (rank, login, time) in new (int rank, string login, int time)[] {
            (rank: 5, login: "eee", time: 10004),
            (rank: 7, login: "ggg", time: 10006),
            (rank: 8, login: "hhh", time: 10007),
            (rank: 10, login: "jjj", time: 10009)
        })
        {
            Assert.Contains(changes.ImprovedRecords, record =>
                record.PlayerId == login
             && record.Time == time);
        }

        Assert.Empty(changes.NewRecords);
        Assert.Empty(changes.RemovedRecords);
        Assert.Empty(changes.WorsenRecords);
        Assert.Empty(changes.PushedOffRecords);
    }

    [Fact]
    public void Compare_ImprovedRecordSameRank()
    {
        var exampleLogin = "aaa";
        var examplePreviousRank = 1;
        var examplePreviousTime = 10000;

        var recordsPrev = new RecordSetDetailedRecord[]
        {
            new(rank: examplePreviousRank, login: exampleLogin, time: examplePreviousTime),
            new(rank: 2, login: "bbb", time: 10001),
            new(rank: 3, login: "ccc", time: 10002),
            new(rank: 4, login: "ddd", time: 10003),
            new(rank: 5, login: "eee", time: 10004),
            new(rank: 6, login: "fff", time: 10005),
            new(rank: 7, login: "ggg", time: 10006),
            new(rank: 8, login: "hhh", time: 10007),
            new(rank: 9, login: "iii", time: 10008),
            new(rank: 10, login: "jjj", time: 10009)
        };

        var records = new RecordSetDetailedRecord[]
        {
            new(rank: examplePreviousRank, login: exampleLogin, time: 9999),
            new(rank: 2, login: "bbb", time: 10001),
            new(rank: 3, login: "ccc", time: 10002),
            new(rank: 4, login: "ddd", time: 10003),
            new(rank: 5, login: "eee", time: 10004),
            new(rank: 6, login: "fff", time: 10005),
            new(rank: 7, login: "ggg", time: 10006),
            new(rank: 8, login: "hhh", time: 10007),
            new(rank: 9, login: "iii", time: 10008),
            new(rank: 10, login: "jjj", time: 10009)
        };

        var changes = LeaderboardComparer.Compare(records, recordsPrev)!;

        Assert.NotNull(changes);
        Assert.True(changes.ImprovedRecords.Count() == 1);
        Assert.Contains(changes.ImprovedRecords, record =>
            record.PlayerId == exampleLogin
         && record.Time == examplePreviousTime);
        Assert.Empty(changes.NewRecords);
        Assert.Empty(changes.RemovedRecords);
        Assert.Empty(changes.WorsenRecords);
        Assert.Empty(changes.PushedOffRecords);
    }

    [Fact]
    public void Compare_RemovedRecord()
    {
        var exampleLogin = "aaa";
        var examplePreviousRank = 1;
        var examplePreviousTime = 10000;

        var recordsPrev = new RecordSetDetailedRecord[]
        {
            new(rank: examplePreviousRank, login: exampleLogin, time: examplePreviousTime),
            new(rank: 2, login: "bbb", time: 10001),
            new(rank: 3, login: "ccc", time: 10002),
            new(rank: 4, login: "ddd", time: 10003),
            new(rank: 5, login: "eee", time: 10004),
            new(rank: 6, login: "fff", time: 10005),
            new(rank: 7, login: "ggg", time: 10006),
            new(rank: 8, login: "hhh", time: 10007),
            new(rank: 9, login: "iii", time: 10008),
            new(rank: 10, login: "jjj", time: 10009)
        };

        var records = new RecordSetDetailedRecord[]
        {
            new(rank: 1, login: "bbb", time: 10001),
            new(rank: 2, login: "ccc", time: 10002),
            new(rank: 3, login: "ddd", time: 10003),
            new(rank: 4, login: "eee", time: 10004),
            new(rank: 5, login: "fff", time: 10005),
            new(rank: 6, login: "ggg", time: 10006),
            new(rank: 7, login: "hhh", time: 10007),
            new(rank: 8, login: "iii", time: 10008),
            new(rank: 9, login: "jjj", time: 10009),
            new(rank: 10, login: "kkk", time: 10010)
        };

        var changes = LeaderboardComparer.Compare(records, recordsPrev)!;

        Assert.NotNull(changes);
        Assert.True(changes.RemovedRecords.Count() == 1);
        Assert.True(changes.NewRecords.Count() == 1);
        Assert.Contains(changes.RemovedRecords, record =>
            record.PlayerId == exampleLogin
         && record.Time == examplePreviousTime);
        Assert.Contains(changes.NewRecords, record =>
            record.PlayerId == "kkk"
         && record.Time == 10010);
        Assert.Empty(changes.ImprovedRecords);
        Assert.Empty(changes.WorsenRecords);
        Assert.Empty(changes.PushedOffRecords);
    }

    [Fact]
    public void Compare_MultipleRemovedRecords()
    {
        var recordsPrev = new RecordSetDetailedRecord[]
        {
            new(rank: 1, login: "aaa", time: 10000),
            new(rank: 2, login: "bbb", time: 10001),
            new(rank: 3, login: "ccc", time: 10002),
            new(rank: 4, login: "ddd", time: 10003),
            new(rank: 5, login: "eee", time: 10004),
            new(rank: 6, login: "fff", time: 10005),
            new(rank: 7, login: "ggg", time: 10006),
            new(rank: 8, login: "hhh", time: 10007),
            new(rank: 9, login: "iii", time: 10008),
            new(rank: 10, login: "jjj", time: 10009)
        };

        var records = new RecordSetDetailedRecord[]
        {
            new(rank: 1, login: "ccc", time: 10002),
            new(rank: 2, login: "ddd", time: 10003),
            new(rank: 3, login: "fff", time: 10005),
            new(rank: 4, login: "ggg", time: 10006),
            new(rank: 5, login: "iii", time: 10008),
            new(rank: 6, login: "jjj", time: 10009),
            new(rank: 7, login: "kkk", time: 10010),
            new(rank: 8, login: "lll", time: 10011),
            new(rank: 9, login: "mmm", time: 10012),
            new(rank: 10, login: "nnn", time: 10013)
        };

        var changes = LeaderboardComparer.Compare(records, recordsPrev)!;

        Assert.NotNull(changes);
        Assert.True(changes.RemovedRecords.Count() == 4);
        Assert.True(changes.NewRecords.Count() == 4);

        foreach (var (rank, login, time) in new (int rank, string login, int time)[] {
            (rank: 1, login: "aaa", time: 10000),
            (rank: 2, login: "bbb", time: 10001),
            (rank: 5, login: "eee", time: 10004),
            (rank: 8, login: "hhh", time: 10007)
        })
        {
            Assert.Contains(changes.RemovedRecords, record =>
                record.PlayerId == login
             && record.Time == time);
        }

        foreach (var (rank, login, time) in new (int rank, string login, int time)[] {
            (rank: 7, login: "kkk", time: 10010),
            (rank: 8, login: "lll", time: 10011),
            (rank: 9, login: "mmm", time: 10012),
            (rank: 10, login: "nnn", time: 10013)
        })
        {
            Assert.Contains(changes.NewRecords, record =>
                record.PlayerId == login
             && record.Time == time);
        }

        Assert.Empty(changes.ImprovedRecords);
        Assert.Empty(changes.WorsenRecords);
        Assert.Empty(changes.PushedOffRecords);
    }

    [Fact]
    public void Compare_WorsenRecord()
    {
        var recordsPrev = new RecordSetDetailedRecord[]
        {
            new(rank: 1, login: "aaa", time: 10000),
            new(rank: 2, login: "bbb", time: 10001),
            new(rank: 3, login: "ccc", time: 10002),
            new(rank: 4, login: "ddd", time: 10003),
            new(rank: 5, login: "eee", time: 10004),
            new(rank: 6, login: "fff", time: 10005),
            new(rank: 7, login: "ggg", time: 10006),
            new(rank: 8, login: "hhh", time: 10007),
            new(rank: 9, login: "iii", time: 10008),
            new(rank: 10, login: "jjj", time: 10009)
        };

        var records = new RecordSetDetailedRecord[]
        {
            new(rank: 1, login: "bbb", time: 10001),
            new(rank: 2, login: "ccc", time: 10002),
            new(rank: 3, login: "ddd", time: 10003),
            new(rank: 4, login: "eee", time: 10004),
            new(rank: 5, login: "aaa", time: 10005),
            new(rank: 6, login: "fff", time: 10005),
            new(rank: 7, login: "ggg", time: 10006),
            new(rank: 8, login: "hhh", time: 10007),
            new(rank: 9, login: "iii", time: 10008),
            new(rank: 10, login: "jjj", time: 10009)
        };

        var changes = LeaderboardComparer.Compare(records, recordsPrev)!;

        Assert.NotNull(changes);
        Assert.True(changes.WorsenRecords.Count() == 1);
        Assert.Contains(changes.WorsenRecords, record =>
            record.PlayerId == "aaa"
         && record.Time == 10000);
        Assert.Empty(changes.NewRecords);
        Assert.Empty(changes.ImprovedRecords);
        Assert.Empty(changes.RemovedRecords);
        Assert.Empty(changes.PushedOffRecords);
    }

    [Fact]
    public void Compare_MultipleWorsenRecords()
    {
        var recordsPrev = new RecordSetDetailedRecord[]
        {
            new(rank: 1, login: "aaa", time: 10000),
            new(rank: 2, login: "bbb", time: 10001),
            new(rank: 3, login: "ccc", time: 10002),
            new(rank: 4, login: "ddd", time: 10003),
            new(rank: 5, login: "eee", time: 10004),
            new(rank: 6, login: "fff", time: 10005),
            new(rank: 7, login: "ggg", time: 10006),
            new(rank: 8, login: "hhh", time: 10007),
            new(rank: 9, login: "iii", time: 10008),
            new(rank: 10, login: "jjj", time: 10009)
        };

        var records = new RecordSetDetailedRecord[]
        {
            new(rank: 1, login: "bbb", time: 10001),
            new(rank: 2, login: "ddd", time: 10003),
            new(rank: 3, login: "ccc", time: 10003),
            new(rank: 4, login: "eee", time: 10004),
            new(rank: 5, login: "aaa", time: 10005),
            new(rank: 6, login: "fff", time: 10005),
            new(rank: 7, login: "ggg", time: 10006),
            new(rank: 8, login: "hhh", time: 10007),
            new(rank: 9, login: "jjj", time: 10009),
            new(rank: 10, login: "iii", time: 10009)
        };

        var changes = LeaderboardComparer.Compare(records, recordsPrev)!;

        Assert.NotNull(changes);
        Assert.True(changes.WorsenRecords.Count() == 3);

        foreach (var (rank, login, time) in new (int rank, string login, int time)[] {
            (rank: 1, login: "aaa", time: 10000),
            (rank: 3, login: "ccc", time: 10002),
            (rank: 9, login: "iii", time: 10008)
        })
        {
            Assert.Contains(changes.WorsenRecords, record =>
                record.PlayerId == login
             && record.Time == time);
        }

        Assert.Empty(changes.NewRecords);
        Assert.Empty(changes.ImprovedRecords);
        Assert.Empty(changes.RemovedRecords);
        Assert.Empty(changes.PushedOffRecords);
    }

    [Fact]
    public void Compare_EveryCase()
    {
        var recordsPrev = new RecordSetDetailedRecord[]
        {
            new(rank: 1, login: "aaa", time: 10000),
            new(rank: 2, login: "bbb", time: 10001),
            new(rank: 3, login: "ccc", time: 10002),
            new(rank: 4, login: "ddd", time: 10003),
            new(rank: 5, login: "eee", time: 10004),
            new(rank: 6, login: "fff", time: 10005),
            new(rank: 7, login: "ggg", time: 10006),
            new(rank: 8, login: "hhh", time: 10007),
            new(rank: 9, login: "iii", time: 10008),
            new(rank: 10, login: "jjj", time: 10009)
        };

        var records = new RecordSetDetailedRecord[]
        {
            new(rank: 1, login: "bbb", time: 9999),
            new(rank: 2, login: "aaa", time: 10000),
            new(rank: 3, login: "ddd", time: 10003),
            new(rank: 4, login: "eee", time: 10004),
            new(rank: 4, login: "kkk", time: 10004),
            new(rank: 6, login: "fff", time: 10005),
            new(rank: 7, login: "ggg", time: 10007),
            new(rank: 7, login: "hhh", time: 10007),
            new(rank: 9, login: "iii", time: 10008),
            new(rank: 10, login: "lll", time: 10009)
        };

        var changes = LeaderboardComparer.Compare(records, recordsPrev)!;

        Assert.NotNull(changes);
        Assert.True(changes.NewRecords.Count() == 2);
        Assert.True(changes.ImprovedRecords.Count() == 1);
        Assert.True(changes.RemovedRecords.Count() == 1);
        Assert.True(changes.WorsenRecords.Count() == 1);
        Assert.True(changes.PushedOffRecords.Count() == 1);
        Assert.Contains(changes.NewRecords, record =>
            record.PlayerId == "kkk"
         && record.Time == 10004);
        Assert.Contains(changes.NewRecords, record =>
            record.PlayerId == "lll"
         && record.Time == 10009);
        Assert.Contains(changes.ImprovedRecords, record =>
            record.PlayerId == "bbb"
         && record.Time == 10001);
        Assert.Contains(changes.RemovedRecords, record =>
            record.PlayerId == "ccc"
         && record.Time == 10002);
        Assert.Contains(changes.WorsenRecords, record =>
            record.PlayerId == "ggg"
         && record.Time == 10006);
        Assert.Contains(changes.PushedOffRecords, record =>
            record.PlayerId == "jjj"
         && record.Time == 10009);
    }
}
