
using BigBang1112.WorldRecordReportLib.Enums;

namespace BigBang1112.WorldRecordReportLib.Repos;

public interface ILoginRepo : IRepo<LoginModel>
{
    Task<LoginModel?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<LoginModel> GetOrAddAsync(GameModel game, string name, string nickname, CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, LoginModel>> GetByNamesAsync(Game game, IEnumerable<Guid> accountIds, CancellationToken cancellationToken);
}
