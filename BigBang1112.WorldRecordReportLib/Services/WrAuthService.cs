using BigBang1112.Data;
using BigBang1112.Models.Db;
using BigBang1112.Services;
using BigBang1112.WorldRecordReportLib.Repos;
using BigBang1112.WorldRecordReportLib.Models.Db;

namespace BigBang1112.WorldRecordReportLib.Services;

/// <summary>
/// Service that connects the features of <see cref="WrRepo"/> and <see cref="AccountsRepo"/> together.
/// </summary>
public class WrAuthService
{
    private readonly IWrRepo _wrRepo;
    private readonly AccountService _accountService;

    public WrAuthService(IWrRepo wrRepo, AccountService accountService)
    {
        _wrRepo = wrRepo;
        _accountService = accountService;
    }

    public async Task<(AccountModel? account, List<DiscordWebhookModel>? webhooks)> GetDiscordWebhooksAsync(CancellationToken cancellationToken = default)
    {
        var (account, associatedAccount) = await GetOrCreateAssociatedAccountAsync(cancellationToken);

        if (associatedAccount is null)
        {
            return (account, null);
        }

        return (account, await _wrRepo.GetDiscordWebhooksByAssociatedAccountAsync(associatedAccount, cancellationToken));
    }

    public async Task<(AccountModel? account, DiscordWebhookModel? webhook)> GetDiscordWebhookAsync(Guid webhookGuid, CancellationToken cancellationToken = default)
    {
        var (account, associatedAccount) = await GetOrCreateAssociatedAccountAsync(cancellationToken);

        if (associatedAccount is null)
        {
            return (account, null);
        }

        return (account, await _wrRepo.GetDiscordWebhookByGuidAsync(webhookGuid, cancellationToken));
    }

    public async Task<(AccountModel? account, AssociatedAccountModel? associatedAccount)> GetOrCreateAssociatedAccountAsync(CancellationToken cancellationToken = default)
    {
        var account = await _accountService.GetAccountAsync(cancellationToken);

        if (account is null)
        {
            return (account, null);
        }

        return (account, await _wrRepo.GetOrCreateAssociatedAccountAsync(account, cancellationToken));
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await _wrRepo.SaveAsync(cancellationToken);
        await _accountService.SaveAsync(cancellationToken);
    }
}
