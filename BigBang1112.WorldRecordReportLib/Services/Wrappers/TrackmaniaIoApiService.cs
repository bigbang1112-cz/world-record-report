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
        return await TrackmaniaIO.GetCampaignsAsync(page, cancellationToken);
    }

    public async Task<Campaign> GetCustomCampaignAsync(int clubId, int campaignId, CancellationToken cancellationToken = default)
    {
        return await TrackmaniaIO.GetCustomCampaignAsync(clubId, campaignId, cancellationToken);
    }

    public async Task<Campaign> GetOfficialCampaignAsync(int campaignId, CancellationToken cancellationToken = default)
    {
        return await TrackmaniaIO.GetOfficialCampaignAsync(campaignId, cancellationToken);
    }

    public async Task<Leaderboard> GetLeaderboardAsync(string leaderboardUid, string mapUid, CancellationToken cancellationToken = default)
    {
        return await TrackmaniaIO.GetLeaderboardAsync(leaderboardUid, mapUid, cancellationToken);
    }

    public async Task<WorldRecord[]> GetRecentWorldRecordsAsync(string leaderboardUid, CancellationToken cancellationToken = default)
    {
        return await TrackmaniaIO.GetRecentWorldRecordsAsync(leaderboardUid, cancellationToken);
    }

}
