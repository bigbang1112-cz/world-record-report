namespace BigBang1112.WorldRecordReportLib.Repos;

public interface IMapGroupRepo : IRepo<MapGroupModel>
{
    Task<IEnumerable<MapGroupModel>> GetAllOrderedAsync(CancellationToken cancellationToken = default);
}
