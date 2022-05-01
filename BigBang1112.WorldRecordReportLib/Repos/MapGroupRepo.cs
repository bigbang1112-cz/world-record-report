using EFCoreSecondLevelCacheInterceptor;
using Microsoft.EntityFrameworkCore;

namespace BigBang1112.WorldRecordReportLib.Repos;

public class MapGroupRepo : Repo<MapGroupModel>, IMapGroupRepo
{
    private readonly WrContext _context;

    public MapGroupRepo(WrContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<string>> GetAllNamesLikeAsync(string value, int limit = 25, CancellationToken cancellationToken = default)
    {
        return await _context.MapGroups.Select(x => x.DisplayName!)
            .Where(x => x != null && x.Contains(value))
            .Distinct()
            .OrderByDescending(x => x.StartsWith(value))
            .ThenBy(x => x)
            .Take(limit)
            .Cacheable()
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<MapGroupModel>> GetAllOrderedAsync(CancellationToken cancellationToken = default)
    {
        return await _context.MapGroups
            .Include(x => x.TitlePack)
                .ThenInclude(x => x!.Author)
            .Include(x => x.Maps)
            .OrderBy(x => x.TitlePack)
            .ThenBy(x => x.Number)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
    }
}
