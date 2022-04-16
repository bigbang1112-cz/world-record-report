
using BigBang1112.WorldRecordReportLib.Enums;

namespace BigBang1112.WorldRecordReportLib.Repos;

public interface IIgnoredLoginsRepo : IRepo<IgnoredLoginModel>
{
    IgnoredLoginModel? GetByLogin(LoginModel login);
    Task<IgnoredLoginModel?> GetByLoginAsync(LoginModel login, CancellationToken cancellationToken = default);
    IgnoredLoginModel? GetByLoginName(string loginName);
    Task<IgnoredLoginModel?> GetByLoginNameAsync(string name, CancellationToken cancellationToken = default);
    IEnumerable<IgnoredLoginModel?> GetByGame(Game game);
    Task<IEnumerable<IgnoredLoginModel>> GetByGameAsync(Game game, CancellationToken cancellationToken = default);
    IEnumerable<string> GetNamesByGame(Game game);
    Task<IEnumerable<string>> GetNamesByGameAsync(Game game, CancellationToken cancellationToken = default);
}
