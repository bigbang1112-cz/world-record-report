using Microsoft.EntityFrameworkCore;

namespace BigBang1112.WorldRecordReportLib.Repos;

public class MapRepo : Repo<MapModel>, IMapRepo
{
    private readonly WrContext _context;

    public MapRepo(WrContext context) : base(context)
    {
        _context = context;
    }

    public async Task<List<MapModel>> GetByCampaignAsync(CampaignModel campaign, CancellationToken cancellationToken = default)
    {
        return await _context.Maps.Where(x => x.Campaign == campaign).ToListAsync(cancellationToken);
    }

    public async Task<MapModel?> GetByUidAsync(string mapUid, CancellationToken cancellationToken = default)
    {
        return await _context.Maps.SingleOrDefaultAsync(x => string.Equals(x.MapUid, mapUid), cancellationToken);
    }

    public async Task<Guid?> GetMapIdByMapUidAsync(string mapUid, CancellationToken cancellationToken = default)
    {
        var map = await _context.Maps.SingleOrDefaultAsync(x => x.MapUid == mapUid, cancellationToken);
        return map?.MapId;
    }
}
