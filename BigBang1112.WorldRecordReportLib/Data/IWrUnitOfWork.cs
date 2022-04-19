using BigBang1112.WorldRecordReportLib.Repos;

namespace BigBang1112.WorldRecordReportLib.Data;

public interface IWrUnitOfWork : IUnitOfWork
{
    ICampaignRepo Campaigns { get; }
    IGameRepo Games { get; }
    IMapRepo Maps { get; }
    IEnvRepo Envs { get; }
    ILoginRepo Logins { get; }
    IMapModeRepo MapModes { get; }
    IWorldRecordRepo WorldRecords { get; }
    IIgnoredLoginRepo IgnoredLogins { get; }
    IDiscordWebhookRepo DiscordWebhooks { get; }
}
