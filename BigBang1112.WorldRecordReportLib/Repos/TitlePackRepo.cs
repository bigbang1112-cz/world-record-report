using EFCoreSecondLevelCacheInterceptor;
using Microsoft.EntityFrameworkCore;

namespace BigBang1112.WorldRecordReportLib.Repos;

public class TitlePackRepo : Repo<TitlePackModel>, ITitlePackRepo
{
    private readonly WrContext _context;

    public TitlePackRepo(WrContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<string>> GetAllUidsLikeAsync(string value, int limit = DiscordConsts.OptionLimit, CancellationToken cancellationToken = default)
    {
        return (await _context.TitlePacks
            .Cacheable(CacheExpirationMode.Absolute, TimeSpan.FromMinutes(1))
            .OrderByDescending(x => x.Name.StartsWith(value))
            .ThenBy(x => x.Id)
            .Take(limit)
            .ToListAsync(cancellationToken))
            .Select(x => $"{x.Name}@{x.Author.Name}")
            .ToList();
    }

    public async Task<TitlePackModel?> GetByFullUidAsync(string titleId, CancellationToken cancellationToken = default)
    {
        var split = titleId.Split('@');

        return await _context.TitlePacks
            .FirstOrDefaultAsync(x => x.Name == split[0] && x.Author.Name == split[1], cancellationToken);
    }
}
