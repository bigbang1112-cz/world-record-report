using BigBang1112.WorldRecordReportLib.Enums;

namespace BigBang1112.WorldRecordReportLib.Repos;

public interface IWorldRecordRepo : IRepo<WorldRecordModel>
{
    Task<WorldRecordModel?> GetCurrentByMapUidAsync(string mapUid, CancellationToken cancellationToken = default);
    Task<IEnumerable<MapModel>> GetAllMapsOfPlayerAsync(LoginModel loginModel, CancellationToken cancellationToken = default);
    Task<IEnumerable<WorldRecordModel>> GetLatestByGameAsync(Game game, int count, CancellationToken cancellationToken = default);
    Task<IEnumerable<WorldRecordModel>> GetByTmxPlayerAsync(TmxLoginModel tmxPlayer, CancellationToken cancellationToken = default);
}
