using BigBang1112.WorldRecordReportLib.Models;
using System.Diagnostics.CodeAnalysis;

namespace BigBang1112.WorldRecordReportLib.Comparers;

public class RecordSetDetailedRecordLoginComparer : IEqualityComparer<RecordSetDetailedRecord>
{
    public bool Equals(RecordSetDetailedRecord? x, RecordSetDetailedRecord? y)
    {
        if (x is null || y is null)
        {
            return false;
        }

        return x.Login == y.Login;
    }

    public int GetHashCode([DisallowNull] RecordSetDetailedRecord obj)
    {
        return obj.Login?.GetHashCode() ?? string.Empty.GetHashCode();
    }
}
