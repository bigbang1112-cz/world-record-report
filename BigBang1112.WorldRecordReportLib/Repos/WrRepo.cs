using BigBang1112.Data;
using BigBang1112.WorldRecordReportLib.Data;
using BigBang1112.WorldRecordReportLib.Models;
using BigBang1112.WorldRecordReportLib.Models.Db;
using EFCoreSecondLevelCacheInterceptor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BigBang1112.WorldRecordReportLib.Repos;

public class WrRepo : IWrRepo
{
    private readonly WrContext _db;
    private readonly ILogger<WrRepo> _logger;

    public WrRepo(WrContext db, ILogger<WrRepo> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<MapModel?> GetMapByUidAsync(string mapUid, CancellationToken cancellationToken = default)
    {
        return await _db.Maps.Cacheable().FirstOrDefaultAsync(x => x.MapUid == mapUid, cancellationToken);
    }

    public async Task<bool> HasRecordCountAsync(MapModel map, CancellationToken cancellationToken = default)
    {
        return await _db.RecordCounts.Cacheable().AnyAsync(x => x.Map == map, cancellationToken);
    }

    public async Task<GameModel> GetTM2GameAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Games.Cacheable().FirstAsync(x => x.Name == NameConsts.GameTM2Name, cancellationToken);
    }

    public async Task<LoginModel> GetOrAddLoginAsync(string login, string? nickname, GameModel game, CancellationToken cancellationToken = default)
    {
        return await _db.Logins.FirstOrAddAsync(x => x.Name == login, () => new LoginModel
        {
            Name = login,
            Nickname = nickname,
            Game = game
        }, cancellationToken: cancellationToken);
    }

    public async Task AddRecordSetDetailedChangesAsync(IEnumerable<RecordSetDetailedChangeModel> changes, CancellationToken cancellationToken = default)
    {
        await _db.RecordSetDetailedChanges.AddRangeAsync(changes, cancellationToken);
    }

    public async Task<int> SaveAsync(CancellationToken cancellationToken = default)
    {
        return await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<string>> GetIgnoredLoginsFromRemovedRecordReportAsync(CancellationToken cancellationToken = default)
    {
        return await _db.IgnoredLoginsFromRemovedRecordReport
            .Select(x => x.Login)
            .Cacheable()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DiscordWebhookModel>> GetDiscordWebhooksAsync(CancellationToken cancellationToken = default)
    {
        return await _db.DiscordWebhooks
            .Cacheable(CacheExpirationMode.Absolute, TimeSpan.FromMinutes(1))
            .ToListAsync(cancellationToken);
    }

    public async Task<DiscordWebhookModel?> GetDiscordWebhookByGuidAsync(Guid webhookGuid, CancellationToken cancellationToken = default)
    {
        return await _db.DiscordWebhooks
            .Cacheable()
            .FirstOrDefaultAsync(x => x.Guid == webhookGuid, cancellationToken);
    }

    public async Task AddRecordCountAsync(RecordCountModel recordCount, CancellationToken cancellationToken = default)
    {
        await _db.RecordCounts.AddAsync(recordCount, cancellationToken);
    }

    public async Task AddRecordSetChangeAsync(RecordSetChangeModel recordSetChange, CancellationToken cancellationToken = default)
    {
        await _db.RecordSetChanges.AddAsync(recordSetChange, cancellationToken);
    }

    public async Task AddRecordChangesAsync(IEnumerable<RecordChangeModel> recordChanges, CancellationToken cancellationToken = default)
    {
        await _db.RecordChanges.AddRangeAsync(recordChanges, cancellationToken);
    }

    public async Task AddDiscordWebhookMessageAsync(DiscordWebhookMessageModel msg)
    {
        await _db.DiscordWebhookMessages.AddAsync(msg);
    }

    public async Task<List<MapModel>> GetMapsFromMapGroupAsync(MapGroupModel mapGroup, CancellationToken cancellationToken = default)
    {
        return await _db.Maps
            .Where(x => x.Group == mapGroup)
            .Cacheable()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<WorldRecordModel>> GetWorldRecordHistoryFromMapAsync(MapModel map, CancellationToken cancellationToken = default)
    {
        return await _db.WorldRecords
            .Where(x => x.Map == map)
            .OrderByDescending(x => x.PublishedOn)
            .Cacheable(CacheExpirationMode.Absolute, TimeSpan.FromSeconds(10))
            .ToListAsync(cancellationToken);
    }

    public async Task AddWorldRecordAsync(WorldRecordModel wr, CancellationToken cancellationToken = default)
    {
        await _db.WorldRecords.AddAsync(wr, cancellationToken);
    }

    public async Task<RefreshModel?> GetRefreshByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.Refreshes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddReportAsync(ReportModel report, CancellationToken cancellationToken = default)
    {
        await _db.Reports.AddAsync(report, cancellationToken);
    }

    public async Task<LoginModel?> GetLoginAsync(string login, CancellationToken cancellationToken = default)
    {
        return await _db.Logins.FirstOrDefaultAsync(x => x.Name == login, cancellationToken);
    }

    public Task<RefreshLoopModel?> GetRefreshLoopByGuidAsync(Guid guid, CancellationToken cancellationToken = default)
    {
        return _db.RefreshLoops
            .Include(x => x.StartingRefresh)
            .ThenInclude(x => x.MapGroup)
            .ThenInclude(x => x!.TitlePack) //
            .Cacheable()
            .FirstOrDefaultAsync(x => x.Guid == guid, cancellationToken);
    }

    public async Task<List<WorldRecordModel>> GetWorldRecordHistoryFromMapGroupAsync(MapGroupModel mapGroup, CancellationToken cancellationToken = default)
    {
        return await _db.WorldRecords
            .Where(x => x.Map.Group == mapGroup)
            .Cacheable(CacheExpirationMode.Absolute, TimeSpan.FromSeconds(10))
            .OrderByDescending(x => x.PublishedOn).ToListAsync(cancellationToken);
    }

    public async Task<ReportModel?> GetReportFromWorldRecordAsync(WorldRecordModel wr, CancellationToken cancellationToken = default)
    {
        return await _db.Reports.Cacheable().FirstOrDefaultAsync(x => x.WorldRecord == wr, cancellationToken);
    }

    public async Task<List<LoginModel>> GetLoginsByGameAsync(GameModel game, CancellationToken cancellationToken = default)
    {
        return await _db.Logins
            .Cacheable(CacheExpirationMode.Absolute, TimeSpan.FromMinutes(1))
            .Where(x => x.Game == game)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<LoginModel>> GetLoginsInTM2Async(CancellationToken cancellationToken = default)
    {
        return await _db.Logins
            .Cacheable(CacheExpirationMode.Absolute, TimeSpan.FromMinutes(1))
            .Where(x => x.Game.Name == NameConsts.GameTM2Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<WorldRecordModel>> GetReportsFromTitlePackAsync(string titleIdPart, string titleAuthorPart, int count, CancellationToken cancellationToken = default)
    {
        return await _db.WorldRecords
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
            .Take(count)
            .Cacheable(CacheExpirationMode.Absolute, TimeSpan.FromMinutes(1))
            .ToListAsync(cancellationToken);
    }

    public async Task<MapModel?> GetMapByMxIdAsync(int mxId, TmxSiteModel site, CancellationToken cancellationToken = default)
    {
        return await _db.Maps
            .Cacheable()
            .FirstOrDefaultAsync(x => x.TmxAuthor != null && x.TmxAuthor.Site == site && x.MxId == mxId, cancellationToken);
    }

    public async Task<TmxSiteModel?> GetTmxSiteByShortNameAsync(string shortName, CancellationToken cancellationToken = default)
    {
        return await _db.TmxSites
            .Cacheable()
            .FirstOrDefaultAsync(x => x.ShortName == shortName, cancellationToken);
    }

    public async Task<TmxSiteModel> GetUnitedTmxAsync(CancellationToken cancellationToken = default)
    {
        return (await GetTmxSiteByShortNameAsync(NameConsts.TMXSiteUnited, cancellationToken))!;
    }

    public async Task<TmxSiteModel> GetTMNFTmxAsync(CancellationToken cancellationToken = default)
    {
        return (await GetTmxSiteByShortNameAsync(NameConsts.TMXSiteTMNF, cancellationToken))!;
    }

    public async Task<TmxLoginModel> GetOrAddTmxLoginAsync(int userId, TmxSiteModel site, CancellationToken cancellationToken = default)
    {
        return await _db.TmxLogins.FirstOrAddAsync(x => x.UserId == userId && x.Site == site, () => new TmxLoginModel
        {
            UserId = userId,
            JoinedOn = DateTime.UtcNow,
            Site = site
        }, cancellationToken: cancellationToken);
    }

    public async Task<List<WorldRecordModel>> GetLastWorldRecordsInTMUFAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _db.WorldRecords
            .Where(x => !x.Ignored && x.Map.Game.Name == NameConsts.GameTMUFName)
            .OrderByDescending(x => x.DrivenOn)
            .Take(count)
            .Include(x => x.Map)
                .ThenInclude(x => x.Mode)
            .Include(x => x.TmxPlayer)
                .ThenInclude(x => x!.Site)
            .Include(x => x.PreviousWorldRecord)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<WorldRecordModel>> GetWorldRecordsByTmxPlayerAsync(TmxLoginModel tmxPlayer, CancellationToken cancellationToken = default)
    {
        return await _db.WorldRecords
            .Where(x => !x.Ignored && x.TmxPlayer == tmxPlayer)
            .OrderByDescending(x => x.DrivenOn)
            .Include(x => x.Map)
                .ThenInclude(x => x.Mode)
            .Include(x => x.TmxPlayer)
                .ThenInclude(x => x!.Site)
            .Include(x => x.PreviousWorldRecord)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DiscordWebhookModel>> GetDiscordWebhooksByAssociatedAccountAsync(AssociatedAccountModel associatedAccount, CancellationToken cancellationToken = default)
    {
        return await _db.DiscordWebhooks
            .Where(x => x.Account.Guid == associatedAccount.Guid)
            .ToListAsync(cancellationToken);
    }

    public async Task<AssociatedAccountModel> GetOrCreateAssociatedAccountAsync(Guid accountGuid, CancellationToken cancellationToken = default)
    {
        return await _db.AssociatedAccounts.FirstOrAddAsync(x => x.Guid == accountGuid, () => new AssociatedAccountModel { Guid = accountGuid }, cancellationToken: cancellationToken);
    }

    public async Task<bool> HasReachedWebhookLimitAsync(AssociatedAccountModel associatedAccount, CancellationToken cancellationToken = default)
    {
        var count = await _db.DiscordWebhooks
            .Where(x => x.Account == associatedAccount)
            .CountAsync(cancellationToken);

        if (count < 5)
        {
            _logger.LogInformation("Account {guid} currently has {count} webhooks.", associatedAccount.Guid, count);
            return false;
        }

        _logger.LogInformation("Account {guid} has reached the webhook limit.", associatedAccount.Guid);

        return true;
    }

    public async Task AddDiscordWebhookAsync(DiscordWebhookModel webhook, CancellationToken cancellationToken = default)
    {
        await _db.DiscordWebhooks.AddAsync(webhook, cancellationToken);
    }

    public async Task<WorldRecordModel?> GetWorldRecordAsync(Guid wrGuid, CancellationToken cancellationToken = default)
    {
        return await _db.WorldRecords
            .Cacheable()
            .FirstOrDefaultAsync(x => x.Guid == wrGuid, cancellationToken);
    }

    public async Task<WorldRecordModel?> GetWorldRecordAsync(MapModel map, CancellationToken cancellationToken = default)
    {
        return await _db.WorldRecords
            .Where(x => x.Map == map && !x.Ignored)
            .OrderByDescending(x => x.PublishedOn)
            .Cacheable(CacheExpirationMode.Absolute, TimeSpan.FromMinutes(1))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<string>> GetMapNamesAsync(string value, int limit = DiscordConsts.OptionLimit, CancellationToken cancellationToken = default)
    {
        return await _db.Maps.Select(x => x.DeformattedName!)
            .Where(x => x.Contains(value))
            .Distinct()
            .OrderBy(x => x)
            .Take(limit)
            .Cacheable()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<string>> GetEnvNamesAsync(string value, int limit = DiscordConsts.OptionLimit, CancellationToken cancellationToken = default)
    {
        return await _db.Environments
            .Where(x => x.Name.Contains(value))
            .OrderBy(x => x.Id)
            .Select(x => x.Name)
            .Take(limit)
            .Cacheable()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<string>> GetMapUidsAsync(string value, int limit = DiscordConsts.OptionLimit, CancellationToken cancellationToken = default)
    {
        return await _db.Maps.Select(x => x.MapUid)
            .Where(x => x.Contains(value))
            .OrderBy(x => x)
            .Take(limit)
            .Cacheable()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<string>> GetTitlePacksAsync(string value, int limit = DiscordConsts.OptionLimit, CancellationToken cancellationToken = default)
    {
        return (await _db.TitlePacks
            .Cacheable(CacheExpirationMode.Absolute, TimeSpan.FromMinutes(1))
            .OrderBy(x => x.Id)
            .Take(limit)
            .ToListAsync(cancellationToken))
            .Select(x => $"{x.Name}@{x.Author.Name}")
            .ToList();
    }

    public async Task<List<string>> GetMapAuthorLoginsAsync(string value, int limit = 25, CancellationToken cancellationToken = default)
    {
        return await _db.Maps.Select(x => x.Author.Name)
            .Where(x => x.Contains(value))
            .Distinct()
            .OrderBy(x => x)
            .Take(limit)
            .Cacheable()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<MapModel>> GetMapsByNameAsync(string mapName, int limit = DiscordConsts.OptionLimit, CancellationToken cancellationToken = default)
    {
        return await _db.Maps
            .Where(x => x.DeformattedName.Contains(mapName))
            .OrderBy(x => x.DeformattedName)
            .Take(limit)
            .Cacheable()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<MapModel>> GetMapsByMultipleParamsAsync(string? mapName = null, string? env = null, string? title = null, string? authorLogin = null, int limit = DiscordConsts.OptionLimit, CancellationToken cancellationToken = default)
    {
        var queryable = _db.Maps.AsQueryable();

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

        return await queryable.OrderBy(x => x.DeformattedName)
            .ThenBy(x => x.TitlePack!.Id)
            .Take(limit)
            .Cacheable()
            .ToListAsync(cancellationToken);
    }

    public async Task<RecordSetDetailedChangeModel?> GetLastRecordSetDetailedChangeOnMapAsync(MapModel map, CancellationToken cancellationToken = default)
    {
        return await _db.RecordSetDetailedChanges
            .Where(x => x.Map == map && x.Type != RecordSetDetailedChangeType.PushedOff)
            .OrderByDescending(x => x.DrivenBefore)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<RecordSetChangeModel?> GetLastRecordSetChangeOnMapAsync(MapModel map, CancellationToken cancellationToken = default)
    {
        return await _db.RecordSetChanges
            .Where(x => x.Map == map)
            .OrderByDescending(x => x.DrivenBefore)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<RecordSetDetailedChangeModel?> GetOldestRecordSetDetailedChangeOnMapAsync(MapModel map, CancellationToken cancellationToken = default)
    {
        return await _db.RecordSetDetailedChanges
            .Where(x => x.Map == map && x.DrivenBefore != null)
            .OrderBy(x => x.DrivenBefore)
            .Cacheable()
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddNicknameChangeAsync(NicknameChangeModel nicknameChangeModel, CancellationToken cancellationToken = default)
    {
        await _db.NicknameChanges.AddAsync(nicknameChangeModel, cancellationToken);
    }

    public async Task<NicknameChangeModel?> GetLatestNicknameChangeByLoginAsync(LoginModel loginModel, CancellationToken cancellationToken = default)
    {
        return await _db.NicknameChanges
            .OrderByDescending(x => x.PreviousLastSeenOn)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<RecordCountModel>> GetRecordCountsOnMapAsync(MapModel map, CancellationToken cancellationToken = default)
    {
        return await _db.RecordCounts.Where(x => x.Map == map)
            .OrderBy(x => x.Before)
            .GroupBy(x => x.Before)
            .Select(x => x.First())
            .ToListAsync(cancellationToken);
    }

    public async Task<DateTime> GetStartingDateOfHistoryTrackingAsync(TitlePackModel titlePack, CancellationToken cancellationToken = default)
    {
        return await _db.WorldRecords
            .Include(x => x.Map)
            .Where(x => x.Map.TitlePack == titlePack && x.PreviousWorldRecord != null && !x.Ignored)
            .OrderBy(x => x.DrivenOn)
            .Select(x => x.DrivenOn)
            .Cacheable()
            .FirstOrDefaultAsync(cancellationToken);
    }
}
