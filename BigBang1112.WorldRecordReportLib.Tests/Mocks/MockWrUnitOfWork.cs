using BigBang1112.Data;
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
}
