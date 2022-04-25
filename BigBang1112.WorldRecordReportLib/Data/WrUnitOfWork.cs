using BigBang1112.WorldRecordReportLib.Repos;
using Microsoft.Extensions.Logging;

namespace BigBang1112.WorldRecordReportLib.Data;

public class WrUnitOfWork : UnitOfWork, IWrUnitOfWork
{
    public IGameRepo Games { get; }
    public ICampaignRepo Campaigns { get; }
    public IMapRepo Maps { get; }
    public IEnvRepo Envs { get; }
    public ILoginRepo Logins { get; }
    public IMapModeRepo MapModes { get; }
    public IWorldRecordRepo WorldRecords { get; }
    public IIgnoredLoginRepo IgnoredLogins { get; }
    public IDiscordWebhookRepo DiscordWebhooks { get; }
    public IReportRepo Reports { get; }
    public ITmxLoginRepo TmxLogins { get; }

    public WrUnitOfWork(WrContext context, ILogger<WrUnitOfWork> logger) : base(context, logger)
    {
        Games = new GameRepo(context);
        Campaigns = new CampaignRepo(context);
        Maps = new MapRepo(context);
        Envs = new EnvRepo(context);
        Logins = new LoginRepo(context);
        MapModes = new MapModeRepo(context);
        WorldRecords = new WorldRecordRepo(context);
        IgnoredLogins = new IgnoredLoginRepo(context);
        DiscordWebhooks = new DiscordWebhookRepo(context);
        Reports = new ReportRepo(context);
        TmxLogins = new TmxLoginRepo(context);
    }
}
