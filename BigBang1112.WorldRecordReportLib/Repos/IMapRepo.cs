namespace BigBang1112.WorldRecordReportLib.Repos;

public interface IMapRepo : IRepo<MapModel>
{
    Task<MapModel?> GetByUidAsync(string mapUid, CancellationToken cancellationToken = default);
}
