﻿using BigBang1112.Data;
using BigBang1112.WorldRecordReportLib.Data;
using BigBang1112.WorldRecordReportLib.Repos;
using BigBang1112.WorldRecordReportLib.Tests.Mocks.Repos;

namespace BigBang1112.WorldRecordReportLib.Tests.Mocks;

public class MockWrUnitOfWork : MockUnitOfWork, IWrUnitOfWork
{
    public ICampaignRepo Campaigns { get; } = new MockCampaignRepo();
    public IGameRepo Games { get; } = new MockGameRepo();
    public IMapRepo Maps { get; } = new MockMapRepo();
    public IEnvRepo Envs { get; } = new MockEnvRepo();
    public ILoginRepo Logins { get; } = new MockLoginRepo();
    public IMapModeRepo MapModes { get; } = new MockMapModeRepo();
    public IWorldRecordRepo WorldRecords { get; } = new MockWorldRecordRepo();
    public IIgnoredLoginRepo IgnoredLogins { get; } = new MockIgnoredLoginRepo();
    public IDiscordWebhookRepo DiscordWebhooks { get; } = new MockDiscordWebhookRepo();
    public IReportRepo Reports { get; } = new MockReportRepo();
    public ITmxLoginRepo TmxLogins { get; } = new MockTmxLoginRepo();
    public IMapGroupRepo MapGroups => throw new NotImplementedException();
    public INicknameChangeRepo NicknameChanges => throw new NotImplementedException();
    public IRecordSetDetailedChangeRepo RecordSetDetailedChanges => throw new NotImplementedException();
    public IRecordCountRepo RecordCounts => throw new NotImplementedException();
    public ITitlePackRepo TitlePacks => throw new NotImplementedException();
    public IDiscordWebhookMessageRepo DiscordWebhookMessages => throw new NotImplementedException();
    public IAssociatedAccountRepo AssociatedAccounts => throw new NotImplementedException();
}
