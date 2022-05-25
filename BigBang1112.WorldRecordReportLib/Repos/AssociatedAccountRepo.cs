namespace BigBang1112.WorldRecordReportLib.Repos;

public class AssociatedAccountRepo : Repo<AssociatedAccountModel>, IAssociatedAccountRepo
{
    private readonly WrContext _context;

    public AssociatedAccountRepo(WrContext context) : base(context)
    {
        _context = context;
    }

    public async Task<AssociatedAccountModel?> GetOrAddAsync(AccountModel account, CancellationToken cancellationToken)
    {
        return await _context.AssociatedAccounts
            .FirstOrAddAsync(x => x.Guid == account.Guid, () => new AssociatedAccountModel
            {
                Guid = account.Guid
            }, cancellationToken: cancellationToken);
    }

    public async Task<AssociatedAccountModel?> GetOrAddAsync(Guid accountGuid, CancellationToken cancellationToken)
    {
        return await _context.AssociatedAccounts
            .FirstOrAddAsync(x => x.Guid == accountGuid, () => new AssociatedAccountModel
            {
                Guid = accountGuid
            }, cancellationToken: cancellationToken);
    }
}
