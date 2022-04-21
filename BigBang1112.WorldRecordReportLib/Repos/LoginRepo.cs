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

    public LoginModel? GetByGameAndName(GameModel game, string name)
    {
        return _context.Logins.SingleOrDefault(x => x.Game == game && string.Equals(x.Name, name));
    }

    public LoginModel GetOrAdd(GameModel game, string name, string nickname)
    {
        var loginModel = GetOrAdd(x => x.Game == game && x.Name == name, () => new LoginModel
        {
            Game = game,
            Name = name,
            JoinedOn = DateTime.UtcNow
        });

        loginModel.Nickname = nickname;
        loginModel.LastSeenOn = DateTime.UtcNow;

        return loginModel;
    }

    public async Task<LoginModel?> GetByGameAndNameAsync(GameModel game, string name, CancellationToken cancellationToken = default)
    {
        return await _context.Logins.SingleOrDefaultAsync(x => x.Game == game && string.Equals(x.Name, name), cancellationToken);
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

    public async Task<Dictionary<Guid, LoginModel>> GetByNamesAsync(Game game, IEnumerable<Guid> accountIds, CancellationToken cancellationToken = default)
    {
        var accountIdsAsString = accountIds.Select(x => x.ToString());
        var logins = await _context.Logins.Where(x => x.Game.Id == (int)game && accountIdsAsString.Contains(x.Name)).ToListAsync(cancellationToken);
        return logins.ToDictionary(x => new Guid(x.Name), x => x);
    }

    public async Task<IEnumerable<string>> GetAllNamesLikeAsync(string value, int? max = null, CancellationToken cancellationToken = default)
    {
        IQueryable<string> queryable = _context.Logins
            .Select(x => x.Name)
            .Where(x => x.Contains(value))
            .OrderByDescending(x => x.StartsWith(value))
            .ThenBy(x => x);

        if (max.HasValue)
        {
            queryable = queryable.Take(max.Value);
        }

        return await queryable.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<string>> GetAllNicknamesLikeAsync(string value, int? max = null, CancellationToken cancellationToken = default)
    {
        IQueryable<string> queryable = _context.Logins
            .Select(x => x.Nickname)
            .Where(x => x != null && x.Contains(value))
            .OfType<string>()
            .OrderByDescending(x => x.StartsWith(value))
            .ThenBy(x => x);

        if (max.HasValue)
        {
            queryable = queryable.Take(max.Value);
        }

        return await queryable.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<string>> GetNamesByNicknameAsync(string? nickname, CancellationToken cancellationToken = default)
    {
        return await _context.Logins
            .Where(x => x.Nickname == nickname)
            .Select(x => x.Name)
            .ToListAsync(cancellationToken);
    }
}
