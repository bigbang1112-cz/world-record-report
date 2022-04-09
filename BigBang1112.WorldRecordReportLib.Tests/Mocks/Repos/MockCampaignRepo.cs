using BigBang1112.Repos.Mocks;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;

namespace BigBang1112.WorldRecordReportLib.Tests.Mocks.Repos;

public class MockCampaignRepo : MockRepo<CampaignModel>, ICampaignRepo
{
    public async Task<CampaignModel?> GetByLeaderboardUidAsync(string leaderboardUid, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(Entities.SingleOrDefault(x => x.LeaderboardUid == leaderboardUid));
    }
}
