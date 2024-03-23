
using BigBang1112.WorldRecordReportLib.Enums;

namespace BigBang1112.WorldRecordReportLib.Repos;

public interface ILoginRepo : IRepo<LoginModel>
{
    Task<LoginModel?> GetByNameAsync(GameModel game, string name, CancellationToken cancellationToken = default);
    Task<LoginModel?> GetByNameAsync(Game game, string name, CancellationToken cancellationToken = default);
    Task<Dictionary<Game, LoginModel>> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<LoginModel?> GetByNicknameAsync(GameModel game, string nickname, CancellationToken cancellationToken = default);
    Task<LoginModel?> GetByNicknameAsync(Game game, string nickname, CancellationToken cancellationToken = default);
    Task<Dictionary<Game, List<LoginModel>>> GetByNicknameAsync(string nickname, CancellationToken cancellationToken = default);
    Task<LoginModel> GetOrAddAsync(GameModel game, string name, string nickname, CancellationToken cancellationToken = default);
    Task<LoginModel> GetOrAddAsync(Game game, string name, string nickname, CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, LoginModel>> GetByNamesAsync(Game game, IEnumerable<Guid> accountIds, CancellationToken cancellationToken = default);
    Task<Dictionary<string, LoginModel>> GetByNamesAsync(Game game, IEnumerable<string> logins, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetAllNamesLikeAsync(string value, int? max = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetAllNicknamesLikeAsync(string value, int? max = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetNamesByNicknameAsync(string? nickname, CancellationToken cancellationToken = default);
    Task<IEnumerable<LoginModel>> GetAllFromTM2Async(CancellationToken cancellationToken = default);
    Task<Dictionary<string, LoginModel>> GetOrAddByNamesAsync(Game game, Dictionary<string, string> loginNicknameDictionary, CancellationToken cancellationToken = default);
}
