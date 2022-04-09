using ManiaAPI.TrackmaniaAPI;
using Microsoft.Extensions.Hosting;

namespace BigBang1112.WorldRecordReportLib.Services.Wrappers;

public interface ITrackmaniaApiService : IHostedService
{
    Task<Dictionary<Guid, string>> GetDisplayNamesAsync(IEnumerable<Guid> accountIds, CancellationToken cancellationToken = default);
    Task<User> GetUserAsync(CancellationToken cancellationToken = default);
}
