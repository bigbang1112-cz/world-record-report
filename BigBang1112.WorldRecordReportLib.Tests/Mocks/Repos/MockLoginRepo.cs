using BigBang1112.Repos.Mocks;
using BigBang1112.WorldRecordReportLib.Enums;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;

namespace BigBang1112.WorldRecordReportLib.Tests.Mocks.Repos;

public class MockLoginRepo : MockRepo<LoginModel>, ILoginRepo
{
    public Task<LoginModel?> GetByNameAsync(GameModel game, string name, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Entities.SingleOrDefault(x => x.Game == game && x.Name == name));
    }

    public async Task<LoginModel> GetOrAddAsync(GameModel game, string name, string nickname, CancellationToken cancellationToken = default)
    {
        var loginModel = await GetOrAddAsync(x => x.Game == game && x.Name == name, () => new LoginModel
        {
            Game = game,
            Name = name,
            JoinedOn = DateTime.UtcNow
        }, cancellationToken);

        loginModel.Nickname = nickname;
        loginModel.LastSeenOn = DateTime.UtcNow;

        return loginModel;
    }

    public async Task<Dictionary<Guid, LoginModel>> GetByNamesAsync(Game game, IEnumerable<Guid> accountIds, CancellationToken cancellationToken)
    {
        var logins = Entities.Where(x => x.Game.Id == (int)game && accountIds.Select(x => x.ToString()).Contains(x.Name));
        return await Task.FromResult(logins.ToDictionary(x => new Guid(x.Name), x => x));
    }

    public Task<IEnumerable<string>> GetAllNamesLikeAsync(string value, int? max = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<string> enumerable = Entities
            .Select(x => x.Name)
            .Where(x => x.Contains(value))
            .OrderByDescending(x => x.StartsWith(value))
            .ThenBy(x => x);

        if (max.HasValue)
        {
            enumerable = enumerable.Take(max.Value);
        }

        return Task.FromResult(enumerable.ToList().AsEnumerable());
    }

    public Task<IEnumerable<string>> GetAllNicknamesLikeAsync(string value, int? max = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<string> enumerable = Entities
           .Select(x => x.Nickname)
           .Where(x => x is not null && x.Contains(value))
           .OfType<string>()
           .OrderByDescending(x => x.StartsWith(value))
           .ThenBy(x => x);

        if (max.HasValue)
        {
            enumerable = enumerable.Take(max.Value);
        }

        return Task.FromResult(enumerable.ToList().AsEnumerable());
    }

    public Task<IEnumerable<string>> GetNamesByNicknameAsync(string? nickname, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Entities
            .Where(x => x.Nickname == nickname)
            .Select(x => x.Name)
            .ToList().AsEnumerable());
    }

    public Task<LoginModel> GetOrAddAsync(Game game, string name, string nickname, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Dictionary<string, LoginModel>> GetByNamesAsync(Game game, IEnumerable<string> logins, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<LoginModel>> GetAllFromTM2Async(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Dictionary<string, LoginModel>> GetOrAddByNamesAsync(Game game, Dictionary<string, string> loginNicknameDictionary, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<LoginModel?> GetByNameAsync(Game game, string name, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Dictionary<Game, LoginModel>> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<Dictionary<Game, List<LoginModel>>> GetByNicknameAsync(string nickname, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
