using BigBang1112.WorldRecordReportLib.Models;
using System.Diagnostics.CodeAnalysis;

namespace BigBang1112.WorldRecordReportLib.Comparers;

public class RecordSetDetailedRecordChangesComparer : IEqualityComparer<TM2Record>
{
    public bool Equals(TM2Record? x, TM2Record? y)
    {
        if (x is null || y is null)
        {
            return false;
        }

        return x.Time == y.Time && x.Login == y.Login && x.ReplayUrl == y.ReplayUrl;
    }

    public int GetHashCode([DisallowNull] TM2Record obj)
    {
        return obj.Login?.GetHashCode() ?? string.Empty.GetHashCode()
            ^ obj.Time.GetHashCode()
            ^ obj.ReplayUrl?.GetHashCode() ?? string.Empty.GetHashCode();
    }
}
