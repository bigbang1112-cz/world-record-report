using EFCoreSecondLevelCacheInterceptor;
using Microsoft.EntityFrameworkCore;

namespace BigBang1112.WorldRecordReportLib.Repos;

public class RecordCountRepo : Repo<RecordCountModel>, IRecordCountRepo
{
    private readonly WrContext _context;

    public RecordCountRepo(WrContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<RecordCountModel>> GetAllByMapAsync(MapModel map, CancellationToken cancellationToken = default)
    {
        return await _context.RecordCounts2.Where(x => x.Map == map)
            .OrderBy(x => x.Before)
            .GroupBy(x => x.Before)
            .Select(x => x.First())
            .ToListAsync(cancellationToken);
    }

    public async Task<DateTime?> GetStartingDateOfTrackingAsync(MapModel map, CancellationToken cancellationToken = default)
    {
        return await _context.RecordCounts2.Where(x => x.Map == map)
            .OrderBy(x => x.Before)
            .Select(x => (DateTime?)x.Before)
            .Cacheable()
            .FirstOrDefaultAsync(cancellationToken);
    }
}
