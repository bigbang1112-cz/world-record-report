namespace BigBang1112.WorldRecordReportLib.Repos;

public interface IMapGroupRepo : IRepo<MapGroupModel>
{
    Task<IEnumerable<MapGroupModel>> GetAllOrderedAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetAllNamesLikeAsync(string value, int limit = 25, CancellationToken cancellationToken = default);
}
