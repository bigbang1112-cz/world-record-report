using BigBang1112.WorldRecordReport.Models;
using System.Diagnostics.CodeAnalysis;

namespace BigBang1112.WorldRecordReport.Comparers;

public class RecordSetDetailedRecordChangesComparer : IEqualityComparer<RecordSetDetailedRecord>
{
    public bool Equals(RecordSetDetailedRecord? x, RecordSetDetailedRecord? y)
    {
        if (x is null || y is null)
        {
            return false;
        }

        return x.Time == y.Time && x.Login == y.Login && x.ReplayUrl == y.ReplayUrl;
    }

    public int GetHashCode([DisallowNull] RecordSetDetailedRecord obj)
    {
        return obj.Login?.GetHashCode() ?? string.Empty.GetHashCode()
            ^ obj.Time.GetHashCode()
            ^ obj.ReplayUrl?.GetHashCode() ?? string.Empty.GetHashCode();
    }
}
