using Microsoft.EntityFrameworkCore;

namespace BigBang1112.WorldRecordReportLib.Repos;

public class NicknameChangeRepo : Repo<NicknameChangeModel>, INicknameChangeRepo
{
    private readonly WrContext _context;

    public NicknameChangeRepo(WrContext context) : base(context)
    {
        _context = context;
    }

    public async Task<NicknameChangeModel?> GetLatestByLoginAsync(LoginModel loginModel, CancellationToken cancellationToken = default)
    {
        return await _context.NicknameChanges
            .OrderByDescending(x => x.PreviousLastSeenOn)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
