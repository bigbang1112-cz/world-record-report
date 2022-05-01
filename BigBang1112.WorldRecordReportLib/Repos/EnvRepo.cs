using BigBang1112.WorldRecordReportLib.Enums;
using EFCoreSecondLevelCacheInterceptor;
using Microsoft.EntityFrameworkCore;

namespace BigBang1112.WorldRecordReportLib.Repos;

public class EnvRepo : EnumRepo<EnvModel, Env>, IEnvRepo
{
    private readonly WrContext _context;

    public EnvRepo(WrContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<string>> GetAllNamesLikeAsync(string value, int limit = DiscordConsts.OptionLimit, CancellationToken cancellationToken = default)
    {
        return await _context.Environments
            .Where(x => x.Name.Contains(value))
            .OrderByDescending(x => x.Name.StartsWith(value))
            .ThenBy(x => x.Id)
            .Select(x => x.Name)
            .Take(limit)
            .Cacheable()
            .ToListAsync(cancellationToken);
    }
}
