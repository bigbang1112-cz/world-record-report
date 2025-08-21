using System.Collections.Immutable;
using ManiaAPI.TrackmaniaIO;
using Microsoft.Extensions.Hosting;

namespace BigBang1112.WorldRecordReportLib.Services.Wrappers;

public interface ITrackmaniaIoApiService
{
    Task<CampaignCollection> GetCampaignsAsync(int page = 0, CancellationToken cancellationToken = default);
    Task<Campaign> GetCustomCampaignAsync(int clubId, int campaignId, CancellationToken cancellationToken = default);
    Task<Leaderboard> GetLeaderboardAsync(string leaderboardUid, string mapUid, CancellationToken cancellationToken = default);
    Task<Campaign> GetOfficialCampaignAsync(int campaignId, CancellationToken cancellationToken = default);
    Task<ImmutableArray<WorldRecord>> GetRecentWorldRecordsAsync(string leaderboardUid, CancellationToken cancellationToken = default);
}
