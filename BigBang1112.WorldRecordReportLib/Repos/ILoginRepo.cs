
namespace BigBang1112.WorldRecordReportLib.Repos;

public interface ILoginRepo : IRepo<LoginModel>
{
    Task<LoginModel?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<LoginModel> GetOrAddAsync(GameModel game, string name, string nickname, CancellationToken cancellationToken = default);
}
