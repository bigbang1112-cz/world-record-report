namespace BigBang1112.WorldRecordReportLib.Repos;

public interface IRecordCountRepo : IRepo<RecordCountModel>
{
    Task<IEnumerable<RecordCountModel>> GetAllByMapAsync(MapModel map, CancellationToken cancellationToken = default);
    Task<DateTime?> GetStartingDateOfTrackingAsync(MapModel map, CancellationToken cancellationToken = default);
}
