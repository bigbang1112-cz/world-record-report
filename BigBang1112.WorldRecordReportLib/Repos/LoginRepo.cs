using BigBang1112.WorldRecordReportLib.Enums;
using Microsoft.EntityFrameworkCore;

namespace BigBang1112.WorldRecordReportLib.Repos;

public class LoginRepo : Repo<LoginModel>, ILoginRepo
{
    private readonly WrContext _context;

    public LoginRepo(WrContext context) : base(context)
    {
        _context = context;
    }

    public LoginModel? GetByName(string name)
    {
        return _context.Logins.SingleOrDefault(x => string.Equals(x.Name, name));
    }

    public LoginModel GetOrAdd(GameModel game, string name, string nickname)
    {
        var loginModel = GetOrAdd<LoginModel>(x => x.Game == game && x.Name == name, () => new LoginModel
        {
            Game = game,
            Name = name,
            JoinedOn = DateTime.UtcNow
        });

        loginModel.Nickname = nickname;
        loginModel.LastSeenOn = DateTime.UtcNow;

        return loginModel;
    }

    public async Task<LoginModel?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Logins.SingleOrDefaultAsync(x => string.Equals(x.Name, name), cancellationToken);
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

    public async Task<Dictionary<Guid, LoginModel>> GetByNamesAsync(Game game, IEnumerable<Guid> accountIds, CancellationToken cancellationToken)
    {
        var accountIdsAsString = accountIds.Select(x => x.ToString());
        var logins = await _context.Logins.Where(x => x.Game.Id == (int)game && accountIdsAsString.Contains(x.Name)).ToListAsync(cancellationToken);
        return logins.ToDictionary(x => new Guid(x.Name), x => x);
    }
}
