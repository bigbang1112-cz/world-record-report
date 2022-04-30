using Microsoft.EntityFrameworkCore;

namespace BigBang1112.WorldRecordReportLib.Repos;

public class MapGroupRepo : Repo<MapGroupModel>, IMapGroupRepo
{
    private readonly WrContext _context;

    public MapGroupRepo(WrContext context) : base(context)
    {
        _context = context;
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
