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
    IReportRepo Reports { get; }
    ITmxLoginRepo TmxLogins { get; }
    IMapGroupRepo MapGroups { get; }
    INicknameChangeRepo NicknameChanges { get; }
    IRecordSetDetailedChangeRepo RecordSetDetailedChanges { get; }
    IRecordCountRepo RecordCounts { get; }
    ITitlePackRepo TitlePacks { get; }
    IDiscordWebhookMessageRepo DiscordWebhookMessages { get; }
    IAssociatedAccountRepo AssociatedAccounts { get; }
}
