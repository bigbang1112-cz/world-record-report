using BigBang1112.WorldRecordReportLib.Enums;
using Microsoft.EntityFrameworkCore;

namespace BigBang1112.WorldRecordReportLib.Repos;

public class GameRepo : EnumRepo<GameModel, Game>, IGameRepo
{
    private readonly WrContext _context;

    public GameRepo(WrContext context) : base(context)
    {
        _context = context;
    }

    public async Task<GameModel?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Games.FirstOrDefaultAsync(x => x.Name == name, cancellationToken);
    }

    public async Task<IEnumerable<string>> GetAllNamesLikeAsync(string value, int? max = null, CancellationToken cancellationToken = default)
    {
        IQueryable<string> queryable = _context.Games
            .Select(x => x.Name)
            .Where(x => x.Contains(value))
            .OrderByDescending(x => x.StartsWith(value))
            .ThenBy(x => x);

        if (max.HasValue)
        {
            queryable = queryable.Take(max.Value);
        }

        return await queryable.ToListAsync(cancellationToken);
    }
}
