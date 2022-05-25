using BigBang1112.Repos.Mocks;
using BigBang1112.WorldRecordReportLib.Enums;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;

namespace BigBang1112.WorldRecordReportLib.Tests.Mocks.Repos;

public class MockIgnoredLoginRepo : MockRepo<IgnoredLoginModel>, IIgnoredLoginRepo
{
    public IEnumerable<IgnoredLoginModel?> GetByGame(Game game)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<IgnoredLoginModel>> GetByGameAsync(Game game, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IgnoredLoginModel? GetByLogin(LoginModel login)
    {
        throw new NotImplementedException();
    }

    public Task<IgnoredLoginModel?> GetByLoginAsync(LoginModel login, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IgnoredLoginModel? GetByLoginName(string loginName)
    {
        throw new NotImplementedException();
    }

    public Task<IgnoredLoginModel?> GetByLoginNameAsync(string name, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<string> GetNamesByGame(Game game)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<string>> GetNamesByGameAsync(Game game, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
