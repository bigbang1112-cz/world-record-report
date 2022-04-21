namespace BigBang1112.WorldRecordReportLib.Repos;

public interface IWorldRecordRepo : IRepo<WorldRecordModel>
{
    Task<WorldRecordModel?> GetCurrentByMapUidAsync(string mapUid, CancellationToken cancellationToken = default);
    Task<IEnumerable<MapModel>> GetAllMapsOfPlayerAsync(LoginModel loginModel, CancellationToken cancellationToken = default);
}
