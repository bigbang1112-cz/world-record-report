using Microsoft.EntityFrameworkCore;

namespace BigBang1112.WorldRecordReportLib.Repos;

public class DiscordWebhookRepo : Repo<DiscordWebhookModel>, IDiscordWebhookRepo
{
    private readonly WrContext _context;

    public DiscordWebhookRepo(WrContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DiscordWebhookModel>> GetAllByAssociatedAccountAsync(AssociatedAccountModel associatedAccount, CancellationToken cancellationToken = default)
    {
        return await _context.DiscordWebhooks
            .Where(x => x.Account.Guid == associatedAccount.Guid)
            .ToListAsync(cancellationToken);
    }

    public async Task<DiscordWebhookModel?> GetByGuidAsync(Guid guid, CancellationToken cancellationToken = default)
    {
        return await _context.DiscordWebhooks
            .FirstOrDefaultAsync(x => x.Guid == guid, cancellationToken);
    }

    public async Task<int> GetCountByAccountAsync(AssociatedAccountModel associatedAccount, CancellationToken cancellationToken = default)
    {
        return await _context.DiscordWebhooks
            .Where(x => x.Account == associatedAccount)
            .CountAsync(cancellationToken);
    }
}
