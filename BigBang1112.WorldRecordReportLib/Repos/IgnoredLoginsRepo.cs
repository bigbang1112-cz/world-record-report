using BigBang1112.WorldRecordReportLib.Enums;
using Microsoft.EntityFrameworkCore;

namespace BigBang1112.WorldRecordReportLib.Repos;

public class IgnoredLoginsRepo : Repo<IgnoredLoginModel>, IIgnoredLoginsRepo
{
    private readonly WrContext _context;

    public IgnoredLoginsRepo(WrContext context) : base(context)
    {
        _context = context;
    }

    public IEnumerable<IgnoredLoginModel?> GetByGame(Game game)
    {
        return _context.IgnoredLogins
            .Where(x => x.Login.Game.Id == (int)game)
            .ToList();
    }

    public async Task<IEnumerable<IgnoredLoginModel>> GetByGameAsync(Game game, CancellationToken cancellationToken = default)
    {
        return await _context.IgnoredLogins
            .Where(x => x.Login.Game.Id == (int)game)
            .ToListAsync(cancellationToken);
    }

    public IgnoredLoginModel? GetByLogin(LoginModel login)
    {
        return _context.IgnoredLogins
            .FirstOrDefault(x => x.Login == login);
    }

    public async Task<IgnoredLoginModel?> GetByLoginAsync(LoginModel login, CancellationToken cancellationToken = default)
    {
        return await _context.IgnoredLogins
            .FirstOrDefaultAsync(x => x.Login == login, cancellationToken);
    }

    public IgnoredLoginModel? GetByLoginName(string loginName)
    {
        return _context.IgnoredLogins
            .FirstOrDefault(x => x.Login.Name == loginName);
    }

    public async Task<IgnoredLoginModel?> GetByLoginNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.IgnoredLogins
            .FirstOrDefaultAsync(x => x.Login.Name == name, cancellationToken);
    }

    public IEnumerable<string> GetNamesByGame(Game game)
    {
        return _context.IgnoredLogins
            .Where(x => x.Login.Game.Id == (int)game)
            .Select(x => x.Login.Name)
            .ToList();
    }

    public async Task<IEnumerable<string>> GetNamesByGameAsync(Game game, CancellationToken cancellationToken = default)
    {
        return await _context.IgnoredLogins
            .Where(x => x.Login.Game.Id == (int)game)
            .Select(x => x.Login.Name)
            .ToListAsync(cancellationToken);
    }
}
