using BigBang1112.WorldRecordReportLib.Enums;
using EFCoreSecondLevelCacheInterceptor;
using Microsoft.EntityFrameworkCore;

namespace BigBang1112.WorldRecordReportLib.Repos;

public class WorldRecordRepo : Repo<WorldRecordModel>, IWorldRecordRepo
{
    private readonly WrContext _context;

    public WorldRecordRepo(WrContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<MapModel>> GetAllMapsOfPlayerAsync(LoginModel loginModel, CancellationToken cancellationToken = default)
    {
        return await _context.WorldRecords
            .Where(x => x.Player == loginModel)
            .Select(x => x.Map)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<WorldRecordModel?> GetCurrentByMapUidAsync(string mapUid, CancellationToken cancellationToken = default)
    {
        return await _context.WorldRecords
            .OrderByDescending(x => x.PublishedOn)
            .FirstOrDefaultAsync(x => x.Map.MapUid == mapUid && !x.Ignored, cancellationToken);
    }

    public async Task<WorldRecordModel?> GetCurrentByMapAsync(MapModel map, CancellationToken cancellationToken = default)
    {
        return await _context.WorldRecords
            .OrderByDescending(x => x.PublishedOn)
            .FirstOrDefaultAsync(x => x.Map == map && !x.Ignored, cancellationToken);
    }

    public async Task<IEnumerable<WorldRecordModel>> GetLatestByGameAsync(Game game, int count, CancellationToken cancellationToken = default)
    {
        return await _context.WorldRecords
            .Where(x => !x.Ignored && x.Map.Game.Id == (int)game)
            .OrderByDescending(x => x.DrivenOn)
            .Take(count)
            .Include(x => x.Map)
                .ThenInclude(x => x.Mode)
            .Include(x => x.TmxPlayer)
                .ThenInclude(x => x!.Site)
            .Include(x => x.PreviousWorldRecord)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WorldRecordModel>> GetByTmxPlayerAsync(TmxLoginModel tmxPlayer, CancellationToken cancellationToken = default)
    {
        return await _context.WorldRecords.Where(x => x.TmxPlayer != null && x.TmxPlayer.Id == tmxPlayer.Id).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WorldRecordModel>> GetHistoriesByMapGroupAsync(MapGroupModel mapGroup, CancellationToken cancellationToken = default)
    {
        return await _context.WorldRecords
            .Where(x => x.Map.Group == mapGroup)
            .Include(x => x.Map)
            .OrderByDescending(x => x.PublishedOn).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WorldRecordModel>> GetHistoryByMapAsync(MapModel map, CancellationToken cancellationToken = default)
    {
        return await _context.WorldRecords
            .Where(x => x.Map == map)
            .OrderByDescending(x => x.PublishedOn)
            .ToListAsync(cancellationToken);
    }

    public async Task<WorldRecordModel?> GetByGuidAsync(Guid guid, CancellationToken cancellationToken = default)
    {
        return await _context.WorldRecords
            .Cacheable()
            .FirstOrDefaultAsync(x => x.Guid == guid, cancellationToken);
    }

    public async Task<IEnumerable<string>> GetAllGuidsLikeAsync(string value, int limit = 25, CancellationToken cancellationToken = default)
    {
        return await _context.WorldRecords.Select(x => x.Guid.ToString())
            .Where(x => x.StartsWith(value))
            .Distinct()
            .OrderByDescending(x => x.StartsWith(value))
            .ThenBy(x => x)
            .Take(limit)
            .Cacheable(CacheExpirationMode.Absolute, TimeSpan.FromMinutes(1))
            .ToListAsync(cancellationToken);
    }

    public async Task<DateTime?> GetStartingDateOfHistoryTrackingByTitlePackAsync(TitlePackModel titlePack, CancellationToken cancellationToken = default)
    {
        return await _context.WorldRecords
            .Include(x => x.Map)
            .Where(x => x.Map.TitlePack == titlePack && x.PreviousWorldRecord != null && !x.Ignored)
            .OrderBy(x => x.DrivenOn)
            .Select(x => x.DrivenOn)
            .Cacheable()
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<DateTime?> GetStartingDateOfHistoryTrackingByCampaignAsync(CampaignModel campaign, CancellationToken cancellationToken = default)
    {
        return await _context.WorldRecords
            .Include(x => x.Map)
            .Where(x => x.Map.Campaign == campaign && x.PreviousWorldRecord != null && !x.Ignored)
            .OrderBy(x => x.DrivenOn)
            .Select(x => x.DrivenOn)
            .Cacheable()
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<WorldRecordModel>> GetRecentByTitlePackAsync(string titleIdPart, string titleAuthorPart, int limit, CancellationToken cancellationToken = default)
    {
        return await _context.WorldRecords
            .Include(x => x.Map)
                .ThenInclude(x => x.TitlePack)
                    .ThenInclude(x => x!.Author)
            .Include(x => x.Player)
            .Include(x => x.PreviousWorldRecord)
                .ThenInclude(x => x!.Player)
            .Where(x => !x.Ignored
                && x.Map.TitlePack != null
                && x.Map.TitlePack.Name == titleIdPart
                && x.Map.TitlePack.Author.Name == titleAuthorPart)
            .OrderByDescending(x => x.DrivenOn)
            .Take(limit)
            .Cacheable(CacheExpirationMode.Absolute, TimeSpan.FromMinutes(1))
            .ToListAsync(cancellationToken);
    }

    public async Task<WorldRecordModel?> GetNextAsync(WorldRecordModel wr, CancellationToken cancellationToken = default)
    {
        return await _context.WorldRecords.FirstOrDefaultAsync(x => x.PreviousWorldRecord == wr, cancellationToken);
    }
}
