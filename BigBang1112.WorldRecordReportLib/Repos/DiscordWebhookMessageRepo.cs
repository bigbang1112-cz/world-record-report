using Microsoft.EntityFrameworkCore;

namespace BigBang1112.WorldRecordReportLib.Repos;

public class DiscordWebhookMessageRepo : Repo<DiscordWebhookMessageModel>, IDiscordWebhookMessageRepo
{
    private readonly WrContext _context;

    public DiscordWebhookMessageRepo(WrContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DiscordWebhookMessageModel>> GetAllByReportAsync(ReportModel report, CancellationToken cancellationToken = default)
    {
        return await _context.DiscordWebhookMessages
            .Where(x => x.Report == report)
            .ToListAsync(cancellationToken);
    }
}
