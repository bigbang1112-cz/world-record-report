using BigBang1112.Repos.Mocks;
using BigBang1112.WorldRecordReportLib.Enums;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;

namespace BigBang1112.WorldRecordReportLib.Tests.Mocks.Repos;

public class MockWorldRecordRepo : MockRepo<WorldRecordModel>, IWorldRecordRepo
{
    public Task<IEnumerable<MapModel>> GetAllMapsOfPlayerAsync(LoginModel loginModel, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<WorldRecordModel>> GetByTmxPlayerAsync(TmxLoginModel tmxPlayer, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<WorldRecordModel?> GetCurrentByMapUidAsync(string mapUid, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(Entities.SingleOrDefault(x => x.Map.MapUid == mapUid));
    }

    public Task<IEnumerable<WorldRecordModel>> GetHistoryByMapAsync(MapModel map, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<WorldRecordModel>> GetHistoriesByMapGroupAsync(MapGroupModel mapGroup, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<WorldRecordModel>> GetLatestByGameAsync(Game game, int count, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<WorldRecordModel?> GetCurrentByMapAsync(MapModel map, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<WorldRecordModel?> GetByGuidAsync(Guid guid, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<string>> GetAllGuidsLikeAsync(string value, int limit = 25, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<DateTime?> GetStartingDateOfHistoryTrackingByTitlePackAsync(TitlePackModel titlePack, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<WorldRecordModel>> GetRecentByTitlePackAsync(string titleIdPart, string titleAuthorPart, int limit, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<WorldRecordModel?> GetNextAsync(WorldRecordModel wr, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<DateTime?> GetStartingDateOfHistoryTrackingByCampaignAsync(CampaignModel campaign, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
