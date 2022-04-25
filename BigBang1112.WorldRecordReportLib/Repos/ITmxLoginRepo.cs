using BigBang1112.WorldRecordReportLib.Enums;

namespace BigBang1112.WorldRecordReportLib.Repos;

public interface ITmxLoginRepo : IRepo<TmxLoginModel>
{
    Task<TmxLoginModel> GetOrAddAsync(int userId, TmxSite tmxSite, CancellationToken cancellationToken = default);
}
