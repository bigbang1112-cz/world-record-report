using BigBang1112.WorldRecordReportLib.Enums;

namespace BigBang1112.WorldRecordReportLib.Repos;

public interface IWorldRecordRepo : IRepo<WorldRecordModel>
{
    Task<WorldRecordModel?> GetCurrentByMapUidAsync(string mapUid, CancellationToken cancellationToken = default);
    Task<WorldRecordModel?> GetCurrentByMapAsync(MapModel map, CancellationToken cancellationToken = default);
    Task<IEnumerable<MapModel>> GetAllMapsOfPlayerAsync(LoginModel loginModel, CancellationToken cancellationToken = default);
    Task<IEnumerable<WorldRecordModel>> GetLatestByGameAsync(Game game, int count, CancellationToken cancellationToken = default);
    Task<IEnumerable<WorldRecordModel>> GetByTmxPlayerAsync(TmxLoginModel tmxPlayer, CancellationToken cancellationToken = default);
    Task<IEnumerable<WorldRecordModel>> GetHistoriesByMapGroupAsync(MapGroupModel mapGroup, CancellationToken cancellationToken = default);
    Task<IEnumerable<WorldRecordModel>> GetHistoryByMapAsync(MapModel map, CancellationToken cancellationToken = default);
    Task<WorldRecordModel?> GetByGuidAsync(Guid guid, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetAllGuidsLikeAsync(string value, int limit = 25, CancellationToken cancellationToken = default);
    Task<DateTime?> GetStartingDateOfHistoryTrackingByTitlePackAsync(TitlePackModel titlePack, CancellationToken cancellationToken = default);
    Task<IEnumerable<WorldRecordModel>> GetRecentByTitlePackAsync(string titleIdPart, string titleAuthorPart, int limit, CancellationToken cancellationToken = default);
}
