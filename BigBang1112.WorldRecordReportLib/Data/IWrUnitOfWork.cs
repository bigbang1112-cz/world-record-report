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

    /// <summary>
    /// Discards all pending changes, so a failed operation is not saved by the next <see cref="IUnitOfWork.SaveAsync"/>.
    /// All entities loaded so far become detached and have to be loaded again.
    /// </summary>
    void DiscardChanges();
}
