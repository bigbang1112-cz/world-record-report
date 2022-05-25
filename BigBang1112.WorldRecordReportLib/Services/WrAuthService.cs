using BigBang1112.Data;
using BigBang1112.Models.Db;
using BigBang1112.Services;
using BigBang1112.WorldRecordReportLib.Repos;
using BigBang1112.WorldRecordReportLib.Models.Db;
using Microsoft.Extensions.Logging;

namespace BigBang1112.WorldRecordReportLib.Services;

/// <summary>
/// Service that connects the features of <see cref="WrRepo"/> and <see cref="AccountsRepo"/> together.
/// </summary>
public class WrAuthService
{
    private readonly IWrUnitOfWork _wrUnitOfWork;
    private readonly AccountService _accountService;
    private readonly ILogger<WrAuthService> _logger;

    public WrAuthService(IWrUnitOfWork wrUnitOfWork, AccountService accountService, ILogger<WrAuthService> logger)
    {
        _wrUnitOfWork = wrUnitOfWork;
        _accountService = accountService;
        _logger = logger;
    }

    public async Task<(AccountModel? account, IEnumerable<DiscordWebhookModel>? webhooks)> GetDiscordWebhooksAsync(CancellationToken cancellationToken = default)
    {
        var (account, associatedAccount) = await GetOrCreateAssociatedAccountAsync(cancellationToken);

        if (associatedAccount is null)
        {
            return (account, null);
        }

        return (account, await _wrUnitOfWork.DiscordWebhooks.GetAllByAssociatedAccountAsync(associatedAccount, cancellationToken));
    }

    public async Task<(AccountModel? account, DiscordWebhookModel? webhook)> GetDiscordWebhookAsync(Guid webhookGuid, CancellationToken cancellationToken = default)
    {
        var (account, associatedAccount) = await GetOrCreateAssociatedAccountAsync(cancellationToken);

        if (associatedAccount is null)
        {
            return (account, null);
        }

        return (account, await _wrUnitOfWork.DiscordWebhooks.GetByGuidAsync(webhookGuid, cancellationToken));
    }

    public async Task<(AccountModel? account, AssociatedAccountModel? associatedAccount)> GetOrCreateAssociatedAccountAsync(CancellationToken cancellationToken = default)
    {
        var account = await _accountService.GetAccountAsync(cancellationToken);

        if (account is null)
        {
            return (account, null);
        }

        return (account, await _wrUnitOfWork.AssociatedAccounts.GetOrAddAsync(account, cancellationToken));
    }

    public async Task<bool> HasReachedWebhookLimitAsync(AssociatedAccountModel associatedAccount, CancellationToken cancellationToken = default)
    {
        var count = await _wrUnitOfWork.DiscordWebhooks.GetCountByAccountAsync(associatedAccount, cancellationToken);

        if (count < 5)
        {
            _logger.LogInformation("Account {guid} currently has {count} webhooks.", associatedAccount.Guid, count);
            return false;
        }

        _logger.LogInformation("Account {guid} has reached the webhook limit.", associatedAccount.Guid);

        return true;
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await _wrUnitOfWork.SaveAsync(cancellationToken);
        await _accountService.SaveAsync(cancellationToken);
    }
}
