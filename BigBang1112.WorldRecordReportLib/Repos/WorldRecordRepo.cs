using BigBang1112.WorldRecordReportLib.Enums;
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

    public async Task<IEnumerable<WorldRecordModel>> GetLatestByGameAsync(Game game, int count, CancellationToken cancellationToken = default)
    {
        return await _context.WorldRecords
            .Where(x => !x.Ignored && x.Map.Game.Id == (int)game)
            .OrderByDescending(x => x.DrivenOn)
            .Take(count)
            .Include(x => x.Map)
                .ThenInclude(x => x.Mode)
            .Include(x => x.TmxPlayer)
                .ThenInclude(x => x!.Site)
            .Include(x => x.PreviousWorldRecord)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WorldRecordModel>> GetByTmxPlayerAsync(TmxLoginModel tmxPlayer, CancellationToken cancellationToken = default)
    {
        return await _context.WorldRecords.Where(x => x.TmxPlayer != null && x.TmxPlayer.Id == tmxPlayer.Id).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WorldRecordModel>> GetHistoriesByMapGroupAsync(MapGroupModel mapGroup, CancellationToken cancellationToken = default)
    {
        return await _context.WorldRecords
            .Where(x => x.Map.Group == mapGroup)
            .Include(x => x.Map)
            .OrderByDescending(x => x.PublishedOn).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WorldRecordModel>> GetHistoryByMapAsync(MapModel map, CancellationToken cancellationToken = default)
    {
        return await _context.WorldRecords
            .Where(x => x.Map == map)
            .OrderByDescending(x => x.PublishedOn)
            .ToListAsync(cancellationToken);
    }
}
