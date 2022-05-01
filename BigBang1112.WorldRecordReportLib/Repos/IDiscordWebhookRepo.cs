namespace BigBang1112.WorldRecordReportLib.Repos;

public interface IDiscordWebhookRepo : IRepo<DiscordWebhookModel>
{
    Task<IEnumerable<DiscordWebhookModel>> GetAllByAssociatedAccountAsync(AssociatedAccountModel associatedAccount, CancellationToken cancellationToken = default);
    Task<DiscordWebhookModel?> GetByGuidAsync(Guid guid, CancellationToken cancellationToken = default);
    Task<int> GetCountByAccountAsync(AssociatedAccountModel associatedAccount, CancellationToken cancellationToken = default);
}
