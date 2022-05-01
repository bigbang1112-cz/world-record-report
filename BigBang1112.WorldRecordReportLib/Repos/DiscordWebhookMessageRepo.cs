using Microsoft.EntityFrameworkCore;

namespace BigBang1112.WorldRecordReportLib.Repos;

public class DiscordWebhookMessageRepo : Repo<DiscordWebhookMessageModel>, IDiscordWebhookMessageRepo
{
    public DiscordWebhookMessageRepo(DbContext context) : base(context)
    {
    }
}
