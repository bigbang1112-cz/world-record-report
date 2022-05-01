using BigBang1112.WorldRecordReportLib.Enums;

namespace BigBang1112.WorldRecordReportLib.Repos;

public interface IMapRepo : IRepo<MapModel>
{
    Task<MapModel?> GetByUidAsync(string mapUid, CancellationToken cancellationToken = default);
    Task<Guid?> GetMapIdByMapUidAsync(string mapUid, CancellationToken cancellationToken = default);
    Task<List<MapModel>> GetByCampaignAsync(CampaignModel campaign, CancellationToken cancellationToken = default);
    Task<MapModel?> GetByMxIdAsync(int trackId, TmxSite tmxSite, CancellationToken cancellationToken = default);
    Task<IEnumerable<MapModel>> GetByMultipleParamsAsync(string? mapName = null, string? env = null, string? title = null, string? authorLogin = null, string? authorNickname = null, int limit = 25, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetAllUidsLikeAsync(string value, int limit = 25, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetAllDeformattedNamesLikeAsync(string value, int limit = 25, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetAllAuthorLoginsLikeAsync(string value, int limit = 25, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetAllAuthorNicknamesLikeAsync(string value, int limit = 25, CancellationToken cancellationToken = default);
}
