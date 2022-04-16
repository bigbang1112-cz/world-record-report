using BigBang1112.WorldRecordReportLib.Repos;
using Microsoft.Extensions.Logging;

namespace BigBang1112.WorldRecordReportLib.Data;

public class WrUnitOfWork : UnitOfWork, IWrUnitOfWork
{
    private readonly WrContext _context;

    public IGameRepo Games { get; }
    public ICampaignRepo Campaigns { get; }
    public IMapRepo Maps { get; }
    public IEnvRepo Envs { get; }
    public ILoginRepo Logins { get; }
    public IMapModeRepo MapModes { get; }
    public IWorldRecordRepo WorldRecords { get; }
    public IIgnoredLoginsRepo IgnoredLogins { get; }

    public WrUnitOfWork(WrContext context, ILogger<WrUnitOfWork> logger) : base(context, logger)
    {
        _context = context;

        Games = new GameRepo(_context);
        Campaigns = new CampaignRepo(_context);
        Maps = new MapRepo(_context);
        Envs = new EnvRepo(_context);
        Logins = new LoginRepo(_context);
        MapModes = new MapModeRepo(_context);
        WorldRecords = new WorldRecordRepo(_context);
        IgnoredLogins = new IgnoredLoginsRepo(_context);
    }
}
