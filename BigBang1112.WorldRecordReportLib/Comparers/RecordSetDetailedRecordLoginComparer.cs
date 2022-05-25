using BigBang1112.WorldRecordReportLib.Models;
using System.Diagnostics.CodeAnalysis;

namespace BigBang1112.WorldRecordReportLib.Comparers;

public class RecordSetDetailedRecordLoginComparer : IEqualityComparer<TM2Record>
{
    public bool Equals(TM2Record? x, TM2Record? y)
    {
        if (x is null || y is null)
        {
            return false;
        }

        return x.Login == y.Login;
    }

    public int GetHashCode([DisallowNull] TM2Record obj)
    {
        return obj.Login?.GetHashCode() ?? string.Empty.GetHashCode();
    }
}
