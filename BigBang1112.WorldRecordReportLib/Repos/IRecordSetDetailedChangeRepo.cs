namespace BigBang1112.WorldRecordReportLib.Repos;

public interface IRecordSetDetailedChangeRepo : IRepo<RecordSetDetailedChangeModel>
{
    Task<RecordSetDetailedChangeModel?> GetLatestByMapAsync(MapModel map, CancellationToken cancellationToken = default);
    Task<RecordSetDetailedChangeModel?> GetOldestByMapAsync(MapModel map, CancellationToken cancellationToken = default);
}
