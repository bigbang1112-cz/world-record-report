using BigBang1112.WorldRecordReportLib.Models;
using System.Diagnostics.CodeAnalysis;

namespace BigBang1112.WorldRecordReportLib.Comparers;

public class PlayerIdComparer<TPlayerId> : IEqualityComparer<IRecord<TPlayerId>> where TPlayerId : notnull
{
    public bool Equals(IRecord<TPlayerId>? x, IRecord<TPlayerId>? y)
    {
        if (x is null || y is null)
        {
            return false;
        }

        return x.PlayerId.Equals(y.PlayerId);
    }

    public int GetHashCode([DisallowNull] IRecord<TPlayerId> obj)
    {
        return obj.PlayerId?.GetHashCode() ?? string.Empty.GetHashCode();
    }
}
