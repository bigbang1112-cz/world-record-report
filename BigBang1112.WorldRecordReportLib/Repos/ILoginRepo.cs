
using BigBang1112.WorldRecordReportLib.Enums;

namespace BigBang1112.WorldRecordReportLib.Repos;

public interface ILoginRepo : IRepo<LoginModel>
{
    Task<LoginModel?> GetByGameAndNameAsync(GameModel game, string name, CancellationToken cancellationToken = default);
    Task<LoginModel> GetOrAddAsync(GameModel game, string name, string nickname, CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, LoginModel>> GetByNamesAsync(Game game, IEnumerable<Guid> accountIds, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetAllNamesLikeAsync(string value, int? max = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetAllNicknamesLikeAsync(string value, int? max = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetNamesByNicknameAsync(string? nickname, CancellationToken cancellationToken = default);
}
