using BigBang1112.Repos.Mocks;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;

namespace BigBang1112.WorldRecordReportLib.Tests.Mocks.Repos;

public class MockDiscordWebhookRepo : MockRepo<DiscordWebhookModel>, IDiscordWebhookRepo
{
    public Task<IEnumerable<DiscordWebhookModel>> GetAllByAssociatedAccountAsync(AssociatedAccountModel associatedAccount, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<DiscordWebhookModel?> GetByGuidAsync(Guid guid, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<int> GetCountByAccountAsync(AssociatedAccountModel associatedAccount, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
