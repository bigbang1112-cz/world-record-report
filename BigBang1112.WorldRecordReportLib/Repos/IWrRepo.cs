using BigBang1112.Data;
using BigBang1112.Models.Db;
using BigBang1112.WorldRecordReportLib.Models.Db;

namespace BigBang1112.WorldRecordReportLib.Repos;

public interface IWrRepo
{
    Task AddRecordSetDetailedChangesAsync(IEnumerable<RecordSetDetailedChangeModel> changes, CancellationToken cancellationToken = default);
    Task<MapModel?> GetMapByUidAsync(string mapUid, CancellationToken cancellationToken = default);
    Task<MapModel?> GetMapByMxIdAsync(int mxId, TmxSiteModel site, CancellationToken cancellationToken = default);
    Task<LoginModel> GetOrAddLoginAsync(string login, GameModel game, CancellationToken cancellationToken = default);
    Task<LoginModel?> GetLoginAsync(string login, CancellationToken cancellationToken = default);
    Task<List<LoginModel>> GetLoginsByGameAsync(GameModel game, CancellationToken cancellationToken = default);
    Task<List<LoginModel>> GetLoginsInTM2Async(CancellationToken cancellationToken = default);
    Task<GameModel> GetTM2GameAsync(CancellationToken cancellationToken = default);
    Task<bool> HasRecordCountAsync(MapModel map, CancellationToken cancellationToken = default);
    Task<int> SaveAsync(CancellationToken cancellationToken = default);
    Task<List<string>> GetIgnoredLoginsFromRemovedRecordReportAsync(CancellationToken cancellationToken = default);
    async Task<AssociatedAccountModel> GetOrCreateAssociatedAccountAsync(AccountModel account, CancellationToken cancellationToken = default) => await GetOrCreateAssociatedAccountAsync(account.Guid, cancellationToken);
    Task<AssociatedAccountModel> GetOrCreateAssociatedAccountAsync(Guid accountGuid, CancellationToken cancellationToken = default);
    Task<List<DiscordWebhookModel>> GetDiscordWebhooksAsync(CancellationToken cancellationToken = default);
    Task AddRecordCountAsync(RecordCountModel recordCount, CancellationToken cancellationToken = default);
    Task AddRecordSetChangeAsync(RecordSetChangeModel recordSetChange, CancellationToken cancellationToken = default);
    Task AddRecordChangesAsync(IEnumerable<RecordChangeModel> recordChanges, CancellationToken cancellationToken = default);
    Task AddDiscordWebhookMessageAsync(DiscordWebhookMessageModel msg);
    Task<List<MapModel>> GetMapsFromMapGroupAsync(MapGroupModel mapGroup, CancellationToken cancellationToken = default);
    Task<List<WorldRecordModel>> GetWorldRecordHistoryFromMapAsync(MapModel map, CancellationToken cancellationToken = default);
    Task AddWorldRecordAsync(WorldRecordModel wr, CancellationToken cancellationToken = default);
    Task<RefreshModel?> GetRefreshByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<List<MapModel>> GetMapsByNameAsync(string mapName, int limit = DiscordConsts.OptionLimit, CancellationToken cancellationToken = default);
    Task AddReportAsync(ReportModel report, CancellationToken cancellationToken = default);
    Task<RefreshLoopModel?> GetRefreshLoopByGuidAsync(Guid guid, CancellationToken cancellationToken = default);
    Task<List<WorldRecordModel>> GetWorldRecordHistoryFromMapGroupAsync(MapGroupModel mapGroup, CancellationToken cancellationToken = default);
    Task<ReportModel?> GetReportFromWorldRecordAsync(WorldRecordModel wr, CancellationToken cancellationToken = default);
    Task<List<MapModel>> GetMapsByMultipleParamsAsync(string? mapName = null, string? env = null, string? title = null, int limit = DiscordConsts.OptionLimit, CancellationToken cancellationToken = default);
    Task<List<WorldRecordModel>> GetReportsFromTitlePackAsync(string titleIdPart, string titleAuthorPart, int count, CancellationToken cancellationToken = default);
    Task<TmxSiteModel?> GetTmxSiteByShortNameAsync(string shortName, CancellationToken cancellationToken = default);
    Task<TmxSiteModel> GetUnitedTmxAsync(CancellationToken cancellationToken = default);
    Task<TmxSiteModel> GetTMNFTmxAsync(CancellationToken cancellationToken = default);
    Task<TmxLoginModel> GetOrAddTmxLoginAsync(int userId, TmxSiteModel site, CancellationToken cancellationToken = default);
    Task<List<WorldRecordModel>> GetLastWorldRecordsInTMUFAsync(int count, CancellationToken cancellationToken = default);
    Task<List<WorldRecordModel>> GetWorldRecordsByTmxPlayerAsync(TmxLoginModel tmxPlayer, CancellationToken cancellationToken = default);
    Task<List<DiscordWebhookModel>> GetDiscordWebhooksByAssociatedAccountAsync(AssociatedAccountModel associatedAccount, CancellationToken cancellationToken = default);
    Task<DiscordWebhookModel?> GetDiscordWebhookByGuidAsync(Guid webhookGuid, CancellationToken cancellationToken = default);
    Task<bool> HasReachedWebhookLimitAsync(AssociatedAccountModel associatedAccount, CancellationToken cancellationToken = default);
    Task AddDiscordWebhookAsync(DiscordWebhookModel webhook, CancellationToken cancellationToken = default);
    Task<WorldRecordModel?> GetWorldRecordAsync(Guid wrGuid, CancellationToken cancellationToken = default);
    Task<List<string>> GetMapNamesAsync(string value, int limit = DiscordConsts.OptionLimit, CancellationToken cancellationToken = default);
    Task<List<string>> GetEnvNamesAsync(string value, int limit = DiscordConsts.OptionLimit, CancellationToken cancellationToken = default);
    Task<List<string>> GetMapUidsAsync(string value, int limit = DiscordConsts.OptionLimit, CancellationToken cancellationToken = default);
    Task<List<string>> GetTitlePacksAsync(string value, int limit = DiscordConsts.OptionLimit, CancellationToken cancellationToken = default);
    Task<WorldRecordModel?> GetWorldRecordAsync(MapModel map, CancellationToken cancellationToken = default);
}
