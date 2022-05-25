namespace BigBang1112.WorldRecordReportLib.Repos;

public interface ICampaignRepo : IRepo<CampaignModel>
{
    Task<CampaignModel?> GetByLeaderboardUidAsync(string leaderboardUid, CancellationToken cancellationToken = default);
}
