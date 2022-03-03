using BigBang1112.WorldRecordReport.Models.Db;
using Microsoft.EntityFrameworkCore;

namespace BigBang1112.WorldRecordReport.Data;

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
        return await _db.Maps.FirstOrDefaultAsync(x => x.MapUid == mapUid, cancellationToken);
    }

    public async Task<bool> HasRecordCountAsync(MapModel map, CancellationToken cancellationToken = default)
    {
        return await _db.RecordCounts.AnyAsync(x => x.Map == map, cancellationToken);
    }

    public async Task<GameModel> GetTM2GameAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Games.FirstAsync(x => x.Name == NameConsts.GameTM2Name, cancellationToken);
    }

    public async Task<LoginModel> GetOrAddLoginAsync(string login, GameModel game, CancellationToken cancellationToken = default)
    {
        return await _db.Logins.FirstOrAddAsync(x => x.Name == login, () => new LoginModel
        {
            Name = login,
            Game = game
        }, cancellationToken);
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
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DiscordWebhookModel>> GetDiscordWebhooksAsync(CancellationToken cancellationToken = default)
    {
        return await _db.DiscordWebhooks.ToListAsync(cancellationToken);
    }

    public async Task<DiscordWebhookModel?> GetDiscordWebhookByGuidAsync(Guid webhookGuid, CancellationToken cancellationToken = default)
    {
        return await _db.DiscordWebhooks.FirstOrDefaultAsync(x => x.Guid == webhookGuid, cancellationToken);
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
        return await _db.Maps.Where(x => x.Group == mapGroup).ToListAsync(cancellationToken);
    }

    public async Task<List<WorldRecordModel>> GetWorldRecordHistoryFromMapAsync(MapModel map, CancellationToken cancellationToken = default)
    {
        return await _db.WorldRecords
            .Where(x => x.Map == map)
            .OrderByDescending(x => x.PublishedOn).ToListAsync(cancellationToken);
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
            .FirstOrDefaultAsync(x => x.Guid == guid, cancellationToken);
    }

    public async Task<List<WorldRecordModel>> GetWorldRecordHistoryFromMapGroupAsync(MapGroupModel mapGroup, CancellationToken cancellationToken = default)
    {
        return await _db.WorldRecords
            .Where(x => x.Map.Group == mapGroup)
            .OrderByDescending(x => x.PublishedOn).ToListAsync(cancellationToken);
    }

    public async Task<ReportModel?> GetReportFromWorldRecordAsync(WorldRecordModel wr, CancellationToken cancellationToken = default)
    {
        return await _db.Reports.FirstOrDefaultAsync(x => x.WorldRecord == wr, cancellationToken);
    }

    public async Task<List<LoginModel>> GetLoginsByGameAsync(GameModel game, CancellationToken cancellationToken = default)
    {
        return await _db.Logins.Where(x => x.Game == game).ToListAsync(cancellationToken);
    }

    public async Task<List<LoginModel>> GetLoginsInTM2Async(CancellationToken cancellationToken = default)
    {
        return await _db.Logins.Where(x => x.Game.Name == NameConsts.GameTM2Name).ToListAsync(cancellationToken);
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
            .ToListAsync(cancellationToken);
    }

    public async Task<MapModel?> GetMapByMxIdAsync(int mxId, TmxSiteModel site, CancellationToken cancellationToken = default)
    {
        return await _db.Maps.FirstOrDefaultAsync(x => x.TmxAuthor != null && x.TmxAuthor.Site == site && x.MxId == mxId, cancellationToken);
    }

    public async Task<TmxSiteModel?> GetTmxSiteByShortNameAsync(string shortName, CancellationToken cancellationToken = default)
    {
        return await _db.TmxSites.FirstOrDefaultAsync(x => x.ShortName == shortName, cancellationToken);
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
        }, cancellationToken);
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
        return await _db.DiscordWebhooks.Where(x => x.Account.Guid == associatedAccount.Guid).ToListAsync(cancellationToken);
    }

    public async Task<AssociatedAccountModel> GetOrCreateAssociatedAccountAsync(Guid accountGuid, CancellationToken cancellationToken = default)
    {
        return await _db.AssociatedAccounts.FirstOrAddAsync(x => x.Guid == accountGuid, () => new AssociatedAccountModel { Guid = accountGuid }, cancellationToken);
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
}
