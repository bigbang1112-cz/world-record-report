using System.Collections.Immutable;
using ManiaAPI.TrackmaniaIO;
using Microsoft.Extensions.Logging;

namespace BigBang1112.WorldRecordReportLib.Services.Wrappers;

public class TrackmaniaIoApiService : ITrackmaniaIoApiService
{
    private readonly TrackmaniaIO _trackmaniaIO;
    private readonly ILogger<TrackmaniaApiService> _logger;

    public TrackmaniaIoApiService(TrackmaniaIO trackmaniaIO, ILogger<TrackmaniaApiService> logger)
    {
        _trackmaniaIO = trackmaniaIO;
        _logger = logger;
    }

    public async Task<CampaignCollection> GetCampaignsAsync(int page = 0, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("HTTP request: Campaigns (page={page})", page);

        return await _trackmaniaIO.GetSeasonalCampaignsAsync(page, cancellationToken);
    }

    public async Task<Campaign> GetCustomCampaignAsync(int clubId, int campaignId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("HTTP request: CustomCampaign (clubId={clubId}; campaignId={campaignId})", clubId, campaignId);

        return await _trackmaniaIO.GetClubCampaignAsync(clubId, campaignId, cancellationToken);
    }

    public async Task<Campaign> GetOfficialCampaignAsync(int campaignId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("HTTP request: OfficialCampaign (campaignId={campaignId})", campaignId);

        return await _trackmaniaIO.GetSeasonalCampaignAsync(campaignId, cancellationToken);
    }

    public async Task<Leaderboard> GetLeaderboardAsync(string leaderboardUid, string mapUid, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("HTTP request: Leaderboard (leaderboardUid={leaderboardUid}; mapUid={mapUid})", leaderboardUid, mapUid);

        return await _trackmaniaIO.GetLeaderboardAsync(leaderboardUid, mapUid, cancellationToken);
    }

    public async Task<ImmutableArray<WorldRecord>> GetRecentWorldRecordsAsync(string leaderboardUid, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("HTTP request: RecentWorldRecords (leaderboardUid={leaderboardUid})", leaderboardUid);

        return await _trackmaniaIO.GetRecentWorldRecordsAsync(leaderboardUid, cancellationToken);
    }
}
