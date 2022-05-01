namespace BigBang1112.WorldRecordReportLib.Repos;

public interface IAssociatedAccountRepo : IRepo<AssociatedAccountModel>
{
    Task<AssociatedAccountModel?> GetOrAddAsync(AccountModel account, CancellationToken cancellationToken);
    Task<AssociatedAccountModel?> GetOrAddAsync(Guid accountGuid, CancellationToken cancellationToken);
}
