namespace BigBang1112.WorldRecordReportLib.Repos;

public class DiscordWebhookRepo : Repo<DiscordWebhookModel>, IDiscordWebhookRepo
{
    private readonly WrContext _context;

    public DiscordWebhookRepo(WrContext context) : base(context)
    {
        _context = context;
    }
}
