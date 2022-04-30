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

    public async Task<LoginModel> GetOrAddAsync(Game game, string name, string nickname, CancellationToken cancellationToken = default)
    {
        var loginModel = await GetOrAddAsync(x => x.GameId == (int)game && x.Name == name, () => new LoginModel
        {
            GameId = (int)game,
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

        var logins = await _context.Logins
            .Where(x => x.Game.Id == (int)game && accountIdsAsString.Contains(x.Name))
            .ToListAsync(cancellationToken);
        
        return logins.ToDictionary(x => new Guid(x.Name), x => x);
    }

    public async Task<Dictionary<string, LoginModel>> GetByNamesAsync(Game game, IEnumerable<string> logins, CancellationToken cancellationToken = default)
    {
        var list = await _context.Logins
            .Where(x => x.Game.Id == (int)game && logins.Contains(x.Name))
            .ToListAsync(cancellationToken);

        return list.ToDictionary(x => x.Name, x => x);
    }

    public async Task<Dictionary<string, LoginModel>> GetOrAddByNamesAsync(Game game, Dictionary<string, string> loginNicknameDictionary, CancellationToken cancellationToken = default)
    {
        var existingLoginModels = await _context.Logins
            .Where(x => x.Game.Id == (int)game && loginNicknameDictionary.Keys.Contains(x.Name))
            .ToListAsync(cancellationToken);

        var existingLoginModelDictionary = existingLoginModels.ToDictionary(x => x.Name, x => x);

        var finalDict = new Dictionary<string, LoginModel>();
        var newModels = new List<LoginModel>();

        foreach (var (login, nickname) in loginNicknameDictionary)
        {
            if (existingLoginModelDictionary.TryGetValue(login, out LoginModel? model))
            {
                finalDict.Add(login, model);
                continue;
            }

            model = new LoginModel
            {
                GameId = (int)game,
                Name = login,
                Nickname = nickname,
                LastSeenOn = DateTime.UtcNow
            };

            newModels.Add(model);
            finalDict.Add(login, model);
        }

        if (newModels.Count > 0)
        {
            await AddRangeAsync(newModels, cancellationToken);
        }

        return finalDict;
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

    public async Task<IEnumerable<LoginModel>> GetAllFromTM2Async(CancellationToken cancellationToken = default)
    {
        return await _context.Logins
            .Where(x => x.Game.Id == (int)Game.TM2)
            .ToListAsync(cancellationToken);
    }
}
