using System.Collections.Immutable;
using ManiaAPI.NadeoAPI;
using Microsoft.Extensions.Hosting;

namespace BigBang1112.WorldRecordReportLib.Services.Wrappers;

public interface INadeoApiService : IHostedService
{
    //Task<string> GetAccountDisplayNamesAsync(IEnumerable<Guid> accountIds, CancellationToken cancellationToken = default);
    Task<ImmutableArray<MapRecord>> GetMapRecordsAsync(IEnumerable<Guid> accountIds, IEnumerable<Guid> mapIds, CancellationToken cancellationToken = default);
    Task<TopLeaderboardCollection> GetTopLeaderboardAsync(string mapUid, int length = 10, int offset = 0, bool onlyWorld = true, CancellationToken cancellationToken = default);
    ValueTask<bool> RefreshAsync(CancellationToken cancellationToken = default);
}
