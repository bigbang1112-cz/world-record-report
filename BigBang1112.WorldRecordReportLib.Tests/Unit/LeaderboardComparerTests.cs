using BigBang1112.WorldRecordReportLib.Models;
using TmEssentials;
using Xunit;

namespace BigBang1112.WorldRecordReportLib.Tests.Unit;

public class LeaderboardComparerTests
{
    [Fact]
    public void Compare_NothingChanged()
    {
        var records = Enumerable.Range(1, 10)
            .Select(i => new TM2Record(
                Rank: i,
                Login: new string('a', i),
                Time: new(11111 * i)
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
            .Select(i => new TM2Record(
                Rank: i,
                Login: new string('a', i), //
                Time: new(11111 * i)
            ));

        var records = Enumerable.Range(1, 10)
            .Select(i => new TM2Record(
                Rank: i,
                Login: new string('b', i), //
                Time: new(11111 * i)
            ));

        var changes = LeaderboardComparer.Compare(records, recordsPrev);

        Assert.NotNull(changes);
    }

    [Fact]
    public void Compare_timeInMillisecondssChanged()
    {
        var recordsPrev = Enumerable.Range(1, 10)
            .Select(i => new TM2Record(
                Rank: i,
                Login: new string('a', i),
                Time: new(11111 * i) //
            ));

        var records = Enumerable.Range(1, 10)
            .Select(i => new TM2Record(
                Rank: i,
                Login: new string('a', i),
                Time: new(11112 * i) //
            ));

        var changes = LeaderboardComparer.Compare(records, recordsPrev);

        Assert.NotNull(changes);
    }

    [Fact]
    public void Compare_NewRecord()
    {
        var recordsPrev = new TM2Record[]
        {
            new(Rank: 1, Login: "aaa", Time: new(10000)),
            new(Rank: 2, Login: "bbb", Time: new(10001)),
            new(Rank: 3, Login: "ccc", Time: new(10002)),
            new(Rank: 4, Login: "ddd", Time: new(10003)), //
            new(Rank: 5, Login: "eee", Time: new(10005)), //
            new(Rank: 6, Login: "fff", Time: new(10006)),
            new(Rank: 7, Login: "ggg", Time: new(10007)),
            new(Rank: 8, Login: "hhh", Time: new(10008)),
            new(Rank: 9, Login: "iii", Time: new(10009)),
            new(Rank: 10, Login: "jjj", Time: new(10010))
        };

        var records = new TM2Record[]
        {
            new(Rank: 1, Login: "aaa", Time: new(10000)),
            new(Rank: 2, Login: "bbb", Time: new(10001)),
            new(Rank: 3, Login: "ccc", Time: new(10002)),
            new(Rank: 4, Login: "ddd", Time: new(10003)),
            new(Rank: 5, Login: "zzz", Time: new(10004)), //
            new(Rank: 6, Login: "eee", Time: new(10005)),
            new(Rank: 7, Login: "fff", Time: new(10006)),
            new(Rank: 8, Login: "ggg", Time: new(10007)),
            new(Rank: 9, Login: "hhh", Time: new(10008)),
            new(Rank: 10, Login: "iii", Time: new(10009))
        };

        var changes = LeaderboardComparer.Compare(records, recordsPrev)!;

        Assert.NotNull(changes);
        Assert.True(changes.NewRecords.Count() == 1);
        Assert.True(changes.PushedOffRecords.Count() == 1);
        Assert.Contains(changes.NewRecords, record =>
             record.PlayerId == "zzz"
          && record.Time.TotalMilliseconds == 10004);
        Assert.Contains(changes.PushedOffRecords, record =>
             record.PlayerId == "jjj"
          && record.Time.TotalMilliseconds == 10010);
        Assert.Empty(changes.ImprovedRecords);
        Assert.Empty(changes.RemovedRecords);
        Assert.Empty(changes.WorsenRecords);
    }

    [Fact]
    public void Compare_MultipleNewRecords()
    {
        var recordsPrev = new TM2Record[]
        {
            new(Rank: 1, Login: "aaa", Time: new(10000)),
            new(Rank: 2, Login: "bbb", Time: new(10001)), //
            new(Rank: 3, Login: "ccc", Time: new(10003)), //
            new(Rank: 4, Login: "ddd", Time: new(10004)), //
            new(Rank: 5, Login: "eee", Time: new(10006)), //
            new(Rank: 6, Login: "fff", Time: new(10008)), //
            new(Rank: 7, Login: "ggg", Time: new(10009)),
            new(Rank: 8, Login: "hhh", Time: new(10010)), //
            new(Rank: 9, Login: "iii", Time: new(10012)), //
            new(Rank: 10, Login: "jjj", Time: new(10013))
        };

        var records = new TM2Record[]
        {
            new(Rank: 1, Login: "aaa", Time: new(10000)),
            new(Rank: 2, Login: "bbb", Time: new(10001)),
            new(Rank: 3, Login: "www", Time: new(10002)), //
            new(Rank: 4, Login: "ccc", Time: new(10003)),
            new(Rank: 5, Login: "ddd", Time: new(10004)),
            new(Rank: 6, Login: "xxx", Time: new(10005)), //
            new(Rank: 7, Login: "eee", Time: new(10006)),
            new(Rank: 8, Login: "yyy", Time: new(10007)), //
            new(Rank: 9, Login: "fff", Time: new(10008)),
            new(Rank: 10, Login: "ggg", Time: new(10009))
        };

        var changes = LeaderboardComparer.Compare(records, recordsPrev)!;

        Assert.NotNull(changes);
        Assert.True(changes.NewRecords.Count() == 3);
        Assert.True(changes.PushedOffRecords.Count() == 3);
        
        Assert.Contains(changes.NewRecords, record =>
            record.PlayerId == "www"
         && record.Time.TotalMilliseconds == 10002);
        Assert.Contains(changes.NewRecords, record =>
            record.PlayerId == "xxx"
         && record.Time.TotalMilliseconds == 10005);
        Assert.Contains(changes.NewRecords, record =>
            record.PlayerId == "yyy"
         && record.Time.TotalMilliseconds == 10007);

        foreach (var (Rank, Login, Time) in new (int Rank, string Login, TimeInt32 Time)[] {
            (Rank: 8, Login: "hhh", Time: new(10010)),
            (Rank: 9, Login: "iii", Time: new(10012)),
            (Rank: 10, Login: "jjj", Time: new(10013))
        })
        {
            Assert.Contains(changes.PushedOffRecords, record =>
                record.PlayerId == Login
             && record.Time == Time);
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
        var examplePrevioustimeInMilliseconds = 10003;

        var recordsPrev = new TM2Record[]
        {
            new(Rank: 1, Login: "aaa", Time: new(10000)),
            new(Rank: 2, Login: "bbb", Time: new(10001)),
            new(Rank: 3, Login: "ccc", Time: new(10002)),
            new(Rank: examplePreviousRank, Login: exampleLogin, Time: new(examplePrevioustimeInMilliseconds)),
            new(Rank: 5, Login: "eee", Time: new(10004)),
            new(Rank: 6, Login: "fff", Time: new(10005)),
            new(Rank: 7, Login: "ggg", Time: new(10006)),
            new(Rank: 8, Login: "hhh", Time: new(10007)),
            new(Rank: 9, Login: "iii", Time: new(10008)),
            new(Rank: 10, Login: "jjj", Time: new(10009))
        };

        var records = new TM2Record[]
        {
            new(Rank: 1, Login: exampleLogin, Time: new(9999)),
            new(Rank: 2, Login: "aaa", Time: new(10000)),
            new(Rank: 3, Login: "bbb", Time: new(10001)),
            new(Rank: 4, Login: "ccc", Time: new(10002)),
            new(Rank: 5, Login: "eee", Time: new(10004)),
            new(Rank: 6, Login: "fff", Time: new(10005)),
            new(Rank: 7, Login: "ggg", Time: new(10006)),
            new(Rank: 8, Login: "hhh", Time: new(10007)),
            new(Rank: 9, Login: "iii", Time: new(10008)),
            new(Rank: 10, Login: "jjj", Time: new(10009))
        };

        var changes = LeaderboardComparer.Compare(records, recordsPrev)!;

        Assert.NotNull(changes);
        Assert.True(changes.ImprovedRecords.Count() == 1);
        Assert.Contains(changes.ImprovedRecords, record =>
            record.PlayerId == exampleLogin
         && record.Time.TotalMilliseconds == examplePrevioustimeInMilliseconds);
        Assert.Empty(changes.NewRecords);
        Assert.Empty(changes.RemovedRecords);
        Assert.Empty(changes.WorsenRecords);
        Assert.Empty(changes.PushedOffRecords);
    }

    [Fact]
    public void Compare_MultipleImprovedRecords()
    {
        var recordsPrev = new TM2Record[]
        {
            new(Rank: 1, Login: "aaa", Time: new(10000)),
            new(Rank: 2, Login: "bbb", Time: new(10001)),
            new(Rank: 3, Login: "ccc", Time: new(10002)),
            new(Rank: 4, Login: "ddd", Time: new(10003)),
            new(Rank: 5, Login: "eee", Time: new(10004)), //
            new(Rank: 6, Login: "fff", Time: new(10005)),
            new(Rank: 7, Login: "ggg", Time: new(10006)), //
            new(Rank: 8, Login: "hhh", Time: new(10007)), //
            new(Rank: 9, Login: "iii", Time: new(10008)),
            new(Rank: 10, Login: "jjj", Time: new(10009)) //
        };

        var records = new TM2Record[]
        {
            new(Rank: 1, Login: "ggg", Time: new(9997)), //
            new(Rank: 2, Login: "eee", Time: new(9998)), //
            new(Rank: 3, Login: "jjj", Time: new(9999)), //
            new(Rank: 4, Login: "aaa", Time: new(10000)),
            new(Rank: 5, Login: "bbb", Time: new(10001)),
            new(Rank: 6, Login: "ccc", Time: new(10002)),
            new(Rank: 7, Login: "ddd", Time: new(10003)),
            new(Rank: 8, Login: "hhh", Time: new(10004)), //
            new(Rank: 9, Login: "fff", Time: new(10005)),
            new(Rank: 10, Login: "iii", Time: new(10008))
        };

        var changes = LeaderboardComparer.Compare(records, recordsPrev)!;

        Assert.NotNull(changes);
        Assert.True(changes.ImprovedRecords.Count() == 4);

        // Previous timeInMillisecondss reminder
        foreach (var (Rank, Login, Time) in new (int Rank, string Login, TimeInt32 Time)[] {
            (Rank: 5, Login: "eee", Time: new(10004)),
            (Rank: 7, Login: "ggg", Time: new(10006)),
            (Rank: 8, Login: "hhh", Time: new(10007)),
            (Rank: 10, Login: "jjj", Time: new(10009))
        })
        {
            Assert.Contains(changes.ImprovedRecords, record =>
                record.PlayerId == Login
             && record.Time == Time);
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
        var examplePrevioustimeInMilliseconds = 10000;

        var recordsPrev = new TM2Record[]
        {
            new(Rank: examplePreviousRank, Login: exampleLogin, Time: new(examplePrevioustimeInMilliseconds)),
            new(Rank: 2, Login: "bbb", Time: new(10001)),
            new(Rank: 3, Login: "ccc", Time: new(10002)),
            new(Rank: 4, Login: "ddd", Time: new(10003)),
            new(Rank: 5, Login: "eee", Time: new(10004)),
            new(Rank: 6, Login: "fff", Time: new(10005)),
            new(Rank: 7, Login: "ggg", Time: new(10006)),
            new(Rank: 8, Login: "hhh", Time: new(10007)),
            new(Rank: 9, Login: "iii", Time: new(10008)),
            new(Rank: 10, Login: "jjj", Time: new(10009))
        };

        var records = new TM2Record[]
        {
            new(Rank: examplePreviousRank, Login: exampleLogin, Time: new(9999)),
            new(Rank: 2, Login: "bbb", Time: new(10001)),
            new(Rank: 3, Login: "ccc", Time: new(10002)),
            new(Rank: 4, Login: "ddd", Time: new(10003)),
            new(Rank: 5, Login: "eee", Time: new(10004)),
            new(Rank: 6, Login: "fff", Time: new(10005)),
            new(Rank: 7, Login: "ggg", Time: new(10006)),
            new(Rank: 8, Login: "hhh", Time: new(10007)),
            new(Rank: 9, Login: "iii", Time: new(10008)),
            new(Rank: 10, Login: "jjj", Time: new(10009))
        };

        var changes = LeaderboardComparer.Compare(records, recordsPrev)!;

        Assert.NotNull(changes);
        Assert.True(changes.ImprovedRecords.Count() == 1);
        Assert.Contains(changes.ImprovedRecords, record =>
            record.PlayerId == exampleLogin
         && record.Time.TotalMilliseconds == examplePrevioustimeInMilliseconds);
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
        var examplePrevioustimeInMilliseconds = 10000;

        var recordsPrev = new TM2Record[]
        {
            new(Rank: examplePreviousRank, Login: exampleLogin, Time: new(examplePrevioustimeInMilliseconds)),
            new(Rank: 2, Login: "bbb", Time: new(10001)),
            new(Rank: 3, Login: "ccc", Time: new(10002)),
            new(Rank: 4, Login: "ddd", Time: new(10003)),
            new(Rank: 5, Login: "eee", Time: new(10004)),
            new(Rank: 6, Login: "fff", Time: new(10005)),
            new(Rank: 7, Login: "ggg", Time: new(10006)),
            new(Rank: 8, Login: "hhh", Time: new(10007)),
            new(Rank: 9, Login: "iii", Time: new(10008)),
            new(Rank: 10, Login: "jjj", Time: new(10009))
        };

        var records = new TM2Record[]
        {
            new(Rank: 1, Login: "bbb", Time: new(10001)),
            new(Rank: 2, Login: "ccc", Time: new(10002)),
            new(Rank: 3, Login: "ddd", Time: new(10003)),
            new(Rank: 4, Login: "eee", Time: new(10004)),
            new(Rank: 5, Login: "fff", Time: new(10005)),
            new(Rank: 6, Login: "ggg", Time: new(10006)),
            new(Rank: 7, Login: "hhh", Time: new(10007)),
            new(Rank: 8, Login: "iii", Time: new(10008)),
            new(Rank: 9, Login: "jjj", Time: new(10009)),
            new(Rank: 10, Login: "kkk", Time: new(10010))
        };

        var changes = LeaderboardComparer.Compare(records, recordsPrev)!;

        Assert.NotNull(changes);
        Assert.True(changes.RemovedRecords.Count() == 1);
        Assert.True(changes.NewRecords.Count() == 1);
        Assert.Contains(changes.RemovedRecords, record =>
            record.PlayerId == exampleLogin
         && record.Time.TotalMilliseconds == examplePrevioustimeInMilliseconds);
        Assert.Contains(changes.NewRecords, record =>
            record.PlayerId == "kkk"
         && record.Time.TotalMilliseconds == 10010);
        Assert.Empty(changes.ImprovedRecords);
        Assert.Empty(changes.WorsenRecords);
        Assert.Empty(changes.PushedOffRecords);
    }

    [Fact]
    public void Compare_MultipleRemovedRecords()
    {
        var recordsPrev = new TM2Record[]
        {
            new(Rank: 1, Login: "aaa", Time: new(10000)),
            new(Rank: 2, Login: "bbb", Time: new(10001)),
            new(Rank: 3, Login: "ccc", Time: new(10002)),
            new(Rank: 4, Login: "ddd", Time: new(10003)),
            new(Rank: 5, Login: "eee", Time: new(10004)),
            new(Rank: 6, Login: "fff", Time: new(10005)),
            new(Rank: 7, Login: "ggg", Time: new(10006)),
            new(Rank: 8, Login: "hhh", Time: new(10007)),
            new(Rank: 9, Login: "iii", Time: new(10008)),
            new(Rank: 10, Login: "jjj", Time: new(10009))
        };

        var records = new TM2Record[]
        {
            new(Rank: 1, Login: "ccc", Time: new(10002)),
            new(Rank: 2, Login: "ddd", Time: new(10003)),
            new(Rank: 3, Login: "fff", Time: new(10005)),
            new(Rank: 4, Login: "ggg", Time: new(10006)),
            new(Rank: 5, Login: "iii", Time: new(10008)),
            new(Rank: 6, Login: "jjj", Time: new(10009)),
            new(Rank: 7, Login: "kkk", Time: new(10010)),
            new(Rank: 8, Login: "lll", Time: new(10011)),
            new(Rank: 9, Login: "mmm", Time: new(10012)),
            new(Rank: 10, Login: "nnn", Time: new(10013))
        };

        var changes = LeaderboardComparer.Compare(records, recordsPrev)!;

        Assert.NotNull(changes);
        Assert.True(changes.RemovedRecords.Count() == 4);
        Assert.True(changes.NewRecords.Count() == 4);

        foreach (var (Rank, Login, Time) in new (int Rank, string Login, TimeInt32 Time)[] {
            (Rank: 1, Login: "aaa", Time: new(10000)),
            (Rank: 2, Login: "bbb", Time: new(10001)),
            (Rank: 5, Login: "eee", Time: new(10004)),
            (Rank: 8, Login: "hhh", Time: new(10007))
        })
        {
            Assert.Contains(changes.RemovedRecords, record =>
                record.PlayerId == Login
             && record.Time == Time);
        }

        foreach (var (Rank, Login, Time) in new (int Rank, string Login, TimeInt32 Time)[] {
            (Rank: 7, Login: "kkk", Time: new(10010)),
            (Rank: 8, Login: "lll", Time: new(10011)),
            (Rank: 9, Login: "mmm", Time: new(10012)),
            (Rank: 10, Login: "nnn", Time: new(10013))
        })
        {
            Assert.Contains(changes.NewRecords, record =>
                record.PlayerId == Login
             && record.Time == Time);
        }

        Assert.Empty(changes.ImprovedRecords);
        Assert.Empty(changes.WorsenRecords);
        Assert.Empty(changes.PushedOffRecords);
    }

    [Fact]
    public void Compare_WorsenRecord()
    {
        var recordsPrev = new TM2Record[]
        {
            new(Rank: 1, Login: "aaa", Time: new(10000)),
            new(Rank: 2, Login: "bbb", Time: new(10001)),
            new(Rank: 3, Login: "ccc", Time: new(10002)),
            new(Rank: 4, Login: "ddd", Time: new(10003)),
            new(Rank: 5, Login: "eee", Time: new(10004)),
            new(Rank: 6, Login: "fff", Time: new(10005)),
            new(Rank: 7, Login: "ggg", Time: new(10006)),
            new(Rank: 8, Login: "hhh", Time: new(10007)),
            new(Rank: 9, Login: "iii", Time: new(10008)),
            new(Rank: 10, Login: "jjj", Time: new(10009))
        };

        var records = new TM2Record[]
        {
            new(Rank: 1, Login: "bbb", Time: new(10001)),
            new(Rank: 2, Login: "ccc", Time: new(10002)),
            new(Rank: 3, Login: "ddd", Time: new(10003)),
            new(Rank: 4, Login: "eee", Time: new(10004)),
            new(Rank: 5, Login: "aaa", Time: new(10005)),
            new(Rank: 6, Login: "fff", Time: new(10005)),
            new(Rank: 7, Login: "ggg", Time: new(10006)),
            new(Rank: 8, Login: "hhh", Time: new(10007)),
            new(Rank: 9, Login: "iii", Time: new(10008)),
            new(Rank: 10, Login: "jjj", Time: new(10009))
        };

        var changes = LeaderboardComparer.Compare(records, recordsPrev)!;

        Assert.NotNull(changes);
        Assert.True(changes.WorsenRecords.Count() == 1);
        Assert.Contains(changes.WorsenRecords, record =>
            record.PlayerId == "aaa"
         && record.Time.TotalMilliseconds == 10000);
        Assert.Empty(changes.NewRecords);
        Assert.Empty(changes.ImprovedRecords);
        Assert.Empty(changes.RemovedRecords);
        Assert.Empty(changes.PushedOffRecords);
    }

    [Fact]
    public void Compare_MultipleWorsenRecords()
    {
        var recordsPrev = new TM2Record[]
        {
            new(Rank: 1, Login: "aaa", Time: new(10000)),
            new(Rank: 2, Login: "bbb", Time: new(10001)),
            new(Rank: 3, Login: "ccc", Time: new(10002)),
            new(Rank: 4, Login: "ddd", Time: new(10003)),
            new(Rank: 5, Login: "eee", Time: new(10004)),
            new(Rank: 6, Login: "fff", Time: new(10005)),
            new(Rank: 7, Login: "ggg", Time: new(10006)),
            new(Rank: 8, Login: "hhh", Time: new(10007)),
            new(Rank: 9, Login: "iii", Time: new(10008)),
            new(Rank: 10, Login: "jjj", Time: new(10009))
        };

        var records = new TM2Record[]
        {
            new(Rank: 1, Login: "bbb", Time: new(10001)),
            new(Rank: 2, Login: "ddd", Time: new(10003)),
            new(Rank: 3, Login: "ccc", Time: new(10003)),
            new(Rank: 4, Login: "eee", Time: new(10004)),
            new(Rank: 5, Login: "aaa", Time: new(10005)),
            new(Rank: 6, Login: "fff", Time: new(10005)),
            new(Rank: 7, Login: "ggg", Time: new(10006)),
            new(Rank: 8, Login: "hhh", Time: new(10007)),
            new(Rank: 9, Login: "jjj", Time: new(10009)),
            new(Rank: 10, Login: "iii", Time: new(10009))
        };

        var changes = LeaderboardComparer.Compare(records, recordsPrev)!;

        Assert.NotNull(changes);
        Assert.True(changes.WorsenRecords.Count() == 3);

        foreach (var (Rank, Login, Time) in new (int Rank, string Login, TimeInt32 Time)[] {
            (Rank: 1, Login: "aaa", Time: new(10000)),
            (Rank: 3, Login: "ccc", Time: new(10002)),
            (Rank: 9, Login: "iii", Time: new(10008))
        })
        {
            Assert.Contains(changes.WorsenRecords, record =>
                record.PlayerId == Login
             && record.Time == Time);
        }

        Assert.Empty(changes.NewRecords);
        Assert.Empty(changes.ImprovedRecords);
        Assert.Empty(changes.RemovedRecords);
        Assert.Empty(changes.PushedOffRecords);
    }

    [Fact]
    public void Compare_EveryCase()
    {
        var recordsPrev = new TM2Record[]
        {
            new(Rank: 1, Login: "aaa", Time: new(10000)),
            new(Rank: 2, Login: "bbb", Time: new(10001)),
            new(Rank: 3, Login: "ccc", Time: new(10002)),
            new(Rank: 4, Login: "ddd", Time: new(10003)),
            new(Rank: 5, Login: "eee", Time: new(10004)),
            new(Rank: 6, Login: "fff", Time: new(10005)),
            new(Rank: 7, Login: "ggg", Time: new(10006)),
            new(Rank: 8, Login: "hhh", Time: new(10007)),
            new(Rank: 9, Login: "iii", Time: new(10008)),
            new(Rank: 10, Login: "jjj", Time: new(10009))
        };

        var records = new TM2Record[]
        {
            new(Rank: 1, Login: "bbb", Time: new(9999)),
            new(Rank: 2, Login: "aaa", Time: new(10000)),
            new(Rank: 3, Login: "ddd", Time: new(10003)),
            new(Rank: 4, Login: "eee", Time: new(10004)),
            new(Rank: 4, Login: "kkk", Time: new(10004)),
            new(Rank: 6, Login: "fff", Time: new(10005)),
            new(Rank: 7, Login: "ggg", Time: new(10007)),
            new(Rank: 7, Login: "hhh", Time: new(10007)),
            new(Rank: 9, Login: "iii", Time: new(10008)),
            new(Rank: 10, Login: "lll", Time: new(10009))
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
         && record.Time.TotalMilliseconds == 10004);
        Assert.Contains(changes.NewRecords, record =>
            record.PlayerId == "lll"
         && record.Time.TotalMilliseconds == 10009);
        Assert.Contains(changes.ImprovedRecords, record =>
            record.PlayerId == "bbb"
         && record.Time.TotalMilliseconds == 10001);
        Assert.Contains(changes.RemovedRecords, record =>
            record.PlayerId == "ccc"
         && record.Time.TotalMilliseconds == 10002);
        Assert.Contains(changes.WorsenRecords, record =>
            record.PlayerId == "ggg"
         && record.Time.TotalMilliseconds == 10006);
        Assert.Contains(changes.PushedOffRecords, record =>
            record.PlayerId == "jjj"
         && record.Time.TotalMilliseconds == 10009);
    }
}
