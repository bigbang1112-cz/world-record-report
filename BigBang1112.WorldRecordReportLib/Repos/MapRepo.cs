﻿using Microsoft.EntityFrameworkCore;

namespace BigBang1112.WorldRecordReportLib.Repos;

public class MapRepo : Repo<MapModel>, IMapRepo
{
    private readonly WrContext _context;

    public MapRepo(WrContext context) : base(context)
    {
        _context = context;
    }

    public async Task<MapModel?> GetByUidAsync(string mapUid, CancellationToken cancellationToken = default)
    {
        return await _context.Maps.SingleOrDefaultAsync(x => string.Equals(x.MapUid, mapUid), cancellationToken);
    }
}