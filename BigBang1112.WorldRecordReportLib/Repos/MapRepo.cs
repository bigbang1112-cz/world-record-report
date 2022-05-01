using BigBang1112.WorldRecordReportLib.Enums;
using EFCoreSecondLevelCacheInterceptor;
using Microsoft.EntityFrameworkCore;

namespace BigBang1112.WorldRecordReportLib.Repos;

public class MapRepo : Repo<MapModel>, IMapRepo
{
    private readonly WrContext _context;

    public MapRepo(WrContext context) : base(context)
    {
        _context = context;
    }

    public async Task<List<MapModel>> GetByCampaignAsync(CampaignModel campaign, CancellationToken cancellationToken = default)
    {
        return await _context.Maps.Where(x => x.Campaign == campaign).ToListAsync(cancellationToken);
    }

    public async Task<MapModel?> GetByMxIdAsync(int trackId, TmxSite tmxSite, CancellationToken cancellationToken = default)
    {
        return await _context.Maps.SingleOrDefaultAsync(x => x.TmxAuthor != null && x.TmxAuthor.Site.Id == (int)tmxSite && x.MxId == trackId, cancellationToken);
    }

    public async Task<MapModel?> GetByUidAsync(string mapUid, CancellationToken cancellationToken = default)
    {
        return await _context.Maps.SingleOrDefaultAsync(x => string.Equals(x.MapUid, mapUid), cancellationToken);
    }

    public async Task<Guid?> GetMapIdByMapUidAsync(string mapUid, CancellationToken cancellationToken = default)
    {
        var map = await _context.Maps.SingleOrDefaultAsync(x => x.MapUid == mapUid, cancellationToken);
        return map?.MapId;
    }

    public async Task<IEnumerable<MapModel>> GetByMultipleParamsAsync(string? mapName = null,
                                                                      string? env = null,
                                                                      string? title = null,
                                                                      string? authorLogin = null,
                                                                      string? authorNickname = null,
                                                                      int limit = DiscordConsts.OptionLimit,
                                                                      CancellationToken cancellationToken = default)
    {
        var queryable = _context.Maps.AsQueryable();

        if (mapName is not null)
        {
            queryable = queryable.Where(x => x.DeformattedName.Contains(mapName));
        }

        if (env is not null)
        {
            queryable = queryable.Where(x => x.Environment.Name.Contains(env));
        }

        if (title is not null)
        {
            var split = title.Split('@');

            queryable = queryable.Where(x => x.TitlePack!.Name.Contains(split[0]) && x.TitlePack!.Author.Name.Contains(split[1]));
        }

        if (authorLogin is not null)
        {
            queryable = queryable.Where(x => x.Author.Name.Contains(authorLogin));
        }

        if (authorNickname is not null)
        {
            queryable = queryable.Where(x => x.Author.Nickname!.Contains(authorNickname));
        }

        return await queryable.OrderBy(x => x.DeformattedName)
            .ThenBy(x => x.TitlePack!.Id)
            .Take(limit)
            .Cacheable()
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<string>> GetAllUidsLikeAsync(string value, int limit = DiscordConsts.OptionLimit, CancellationToken cancellationToken = default)
    {
        return await _context.Maps.Select(x => x.MapUid)
            .Where(x => x.Contains(value))
            .OrderByDescending(x => x.StartsWith(value))
            .ThenBy(x => x)
            .Take(limit)
            .Cacheable()
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<string>> GetAllDeformattedNamesLikeAsync(string value, int limit = DiscordConsts.OptionLimit, CancellationToken cancellationToken = default)
    {
        return await _context.Maps.Select(x => x.DeformattedName!)
            .Where(x => x.Contains(value))
            .Distinct()
            .OrderByDescending(x => x.StartsWith(value))
            .ThenBy(x => x)
            .Take(limit)
            .Cacheable()
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<string>> GetAllAuthorLoginsLikeAsync(string value, int limit = 25, CancellationToken cancellationToken = default)
    {
        return await _context.Maps.Select(x => x.Author.Name)
            .Where(x => x.Contains(value))
            .Distinct()
            .OrderByDescending(x => x.StartsWith(value))
            .ThenBy(x => x)
            .Take(limit)
            .Cacheable()
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<string>> GetAllAuthorNicknamesLikeAsync(string value, int limit = 25, CancellationToken cancellationToken = default)
    {
        return await _context.Maps.Select(x => x.Author.Nickname!)
            .Where(x => x != null && x.Contains(value))
            .Distinct()
            .OrderByDescending(x => x.StartsWith(value))
            .ThenBy(x => x)
            .Take(limit)
            .Cacheable()
            .ToListAsync(cancellationToken);
    }
}
