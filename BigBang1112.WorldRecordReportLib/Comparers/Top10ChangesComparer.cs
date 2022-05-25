using BigBang1112.WorldRecordReportLib.Models;
using System.Diagnostics.CodeAnalysis;

namespace BigBang1112.WorldRecordReportLib.Comparers;

public class Top10ChangesComparer<TPlayerId> : IEqualityComparer<IRecord<TPlayerId>> where TPlayerId : notnull
{
    public bool Equals(IRecord<TPlayerId>? x, IRecord<TPlayerId>? y)
    {
        if (x is null || y is null)
        {
            return false;
        }

        return x.Time == y.Time && x.PlayerId.Equals(y.PlayerId);
    }

    public int GetHashCode([DisallowNull] IRecord<TPlayerId> obj)
    {
        return obj.Time.GetHashCode() ^ obj.PlayerId.GetHashCode();
    }
}
