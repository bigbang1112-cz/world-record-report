using BigBang1112.Repos.Mocks;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;

namespace BigBang1112.WorldRecordReportLib.Tests.Mocks.Repos;

public class MockLoginRepo : MockRepo<LoginModel>, ILoginRepo
{
    public Task<LoginModel?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Entities.SingleOrDefault(x => x.Name == name));
    }

    public async Task<LoginModel> GetOrAddAsync(GameModel game, string name, string nickname, CancellationToken cancellationToken = default)
    {
        var loginModel = await GetOrAddAsync<LoginModel>(x => x.Game == game && x.Name == name, () => new LoginModel
        {
            Game = game,
            Name = name,
            JoinedOn = DateTime.UtcNow
        }, cancellationToken);

        loginModel.Nickname = nickname;
        loginModel.LastSeenOn = DateTime.UtcNow;

        return loginModel;
    }
}
