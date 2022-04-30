using System.Collections.ObjectModel;
using TmEssentials;

namespace BigBang1112.WorldRecordReportLib.Models;

public record LeaderboardTM2(IEnumerable<TM2Record> Records, IEnumerable<UniqueRecord> Times)
{
    private int? cachedRecordCount;

    public int GetRecordCount()
    {
        if (cachedRecordCount is null)
        {
            cachedRecordCount = Times.Where(x => x.Time is null).Sum(x => x.Count);
        }

        return cachedRecordCount.Value;
    }
}
