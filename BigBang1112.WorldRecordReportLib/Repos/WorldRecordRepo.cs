using Microsoft.EntityFrameworkCore;

namespace BigBang1112.WorldRecordReportLib.Repos;

public class WorldRecordRepo : Repo<WorldRecordModel>, IWorldRecordRepo
{
    private readonly WrContext _context;

    public WorldRecordRepo(WrContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<MapModel>> GetAllMapsOfPlayerAsync(LoginModel loginModel, CancellationToken cancellationToken = default)
    {
        return await _context.WorldRecords
            .Where(x => x.Player == loginModel)
            .Select(x => x.Map)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<WorldRecordModel?> GetCurrentByMapUidAsync(string mapUid, CancellationToken cancellationToken = default)
    {
        return await _context.WorldRecords
            .OrderByDescending(x => x.PublishedOn)
            .FirstOrDefaultAsync(x => x.Map.MapUid == mapUid && !x.Ignored, cancellationToken);
    }
}
