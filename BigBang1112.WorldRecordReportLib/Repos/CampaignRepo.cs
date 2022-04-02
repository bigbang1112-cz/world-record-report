using Microsoft.EntityFrameworkCore;

namespace BigBang1112.WorldRecordReportLib.Repos;

public class CampaignRepo : Repo<CampaignModel>, ICampaignRepo
{
    private readonly WrContext _context;

    public CampaignRepo(WrContext context) : base(context)
    {
        _context = context;
    }

    public async Task<CampaignModel?> GetByLeaderboardUidAsync(string leaderboardUid, CancellationToken cancellationToken = default)
    {
        return await _context.Campaigns.SingleOrDefaultAsync(x => x.LeaderboardUid == leaderboardUid, cancellationToken);
    }
}
