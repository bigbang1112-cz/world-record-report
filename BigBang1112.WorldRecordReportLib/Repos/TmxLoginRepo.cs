using BigBang1112.WorldRecordReportLib.Enums;
using Microsoft.EntityFrameworkCore;

namespace BigBang1112.WorldRecordReportLib.Repos;

public class TmxLoginRepo : Repo<TmxLoginModel>, ITmxLoginRepo
{
    private readonly WrContext _context;

    public TmxLoginRepo(WrContext context) : base(context)
    {
        _context = context;
    }

    public async Task<TmxLoginModel> GetOrAddAsync(int userId, TmxSite tmxSite, CancellationToken cancellationToken = default)
    {
        var loginModel = await _context.TmxLogins.SingleOrDefaultAsync(x => x.UserId == userId && x.Site.Id == (int)tmxSite, cancellationToken);

        if (loginModel is null)
        {
            var site = await _context.TmxSites.SingleAsync(x => x.Id == (int)tmxSite, cancellationToken);
            
            loginModel = new TmxLoginModel
            {
                UserId = userId,
                LastSeenOn = DateTime.UtcNow,
                Site = site
            };

            await AddAsync(loginModel, cancellationToken);
        }

        return loginModel;
    }
}
