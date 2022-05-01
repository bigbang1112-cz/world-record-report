using BigBang1112.WorldRecordReportLib.Models;
using EFCoreSecondLevelCacheInterceptor;
using Microsoft.EntityFrameworkCore;

namespace BigBang1112.WorldRecordReportLib.Repos;

public class RecordSetDetailedChangeRepo : Repo<RecordSetDetailedChangeModel>, IRecordSetDetailedChangeRepo
{
    private readonly WrContext _context;

    public RecordSetDetailedChangeRepo(WrContext context) : base(context)
    {
        _context = context;
    }

    public async Task<RecordSetDetailedChangeModel?> GetLatestByMapAsync(MapModel map, CancellationToken cancellationToken = default)
    {
        return await _context.RecordSetDetailedChanges
            .Where(x => x.Map == map && x.Type != RecordSetDetailedChangeType.PushedOff)
            .OrderByDescending(x => x.DrivenBefore)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<RecordSetDetailedChangeModel?> GetOldestByMapAsync(MapModel map, CancellationToken cancellationToken = default)
    {
        return await _context.RecordSetDetailedChanges
            .Where(x => x.Map == map && x.DrivenBefore != null)
            .OrderBy(x => x.DrivenBefore)
            .Cacheable()
            .FirstOrDefaultAsync(cancellationToken);
    }
}
