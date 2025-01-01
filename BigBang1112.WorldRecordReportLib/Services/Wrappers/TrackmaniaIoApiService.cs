using ManiaAPI.TrackmaniaIO;
using Microsoft.Extensions.Logging;

namespace BigBang1112.WorldRecordReportLib.Services.Wrappers;

public class TrackmaniaIoApiService : ITrackmaniaIoApiService
{
    private readonly ILogger<TrackmaniaApiService> _logger;

    public TrackmaniaIoApiService(ILogger<TrackmaniaApiService> logger)
    {
        _logger = logger;
    }

    public async Task<CampaignCollection> GetCampaignsAsync(int page = 0, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("HTTP request: Campaigns (page={page})", page);

        return await TrackmaniaIO.GetSeasonalCampaignsAsync(page, cancellationToken);
    }

    public async Task<Campaign> GetCustomCampaignAsync(int clubId, int campaignId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("HTTP request: CustomCampaign (clubId={clubId}; campaignId={campaignId})", clubId, campaignId);

        return await TrackmaniaIO.GetCustomCampaignAsync(clubId, campaignId, cancellationToken);
    }

    public async Task<Campaign> GetOfficialCampaignAsync(int campaignId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("HTTP request: OfficialCampaign (campaignId={campaignId})", campaignId);

        return await TrackmaniaIO.GetSeasonalCampaignAsync(campaignId, cancellationToken);
    }

    public async Task<Leaderboard> GetLeaderboardAsync(string leaderboardUid, string mapUid, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("HTTP request: Leaderboard (leaderboardUid={leaderboardUid}; mapUid={mapUid})", leaderboardUid, mapUid);

        return await TrackmaniaIO.GetLeaderboardAsync(leaderboardUid, mapUid, cancellationToken);
    }

    public async Task<WorldRecord[]> GetRecentWorldRecordsAsync(string leaderboardUid, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("HTTP request: RecentWorldRecords (leaderboardUid={leaderboardUid})", leaderboardUid);

        return await TrackmaniaIO.GetRecentWorldRecordsAsync(leaderboardUid, cancellationToken);
    }
}
