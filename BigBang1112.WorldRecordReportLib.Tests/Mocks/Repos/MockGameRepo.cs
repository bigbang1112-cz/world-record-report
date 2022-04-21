using System.Linq.Expressions;
using BigBang1112.Repos.Mocks;
using BigBang1112.WorldRecordReportLib.Attributes;
using BigBang1112.WorldRecordReportLib.Enums;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;

namespace BigBang1112.WorldRecordReportLib.Tests.Mocks.Repos;

public class MockGameRepo : MockEnumRepo<GameModel, Game>, IGameRepo
{
    public MockGameRepo()
    {
        Entities = EnumData.Create<GameModel, Game, GameAttribute>(WrEnumData.GameAttributeToModel).ToList();
    }

    public Task<GameModel?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Entities.FirstOrDefault(x => x.Name == name));
    }

    public Task<IEnumerable<string>> GetAllNamesLikeAsync(string value, int? max = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<string> enumerable = Entities
            .Select(x => x.Name)
            .Where(x => x.Contains(value))
            .OrderByDescending(x => x.StartsWith(value))
            .ThenBy(x => x);

        if (max.HasValue)
        {
            enumerable = enumerable.Take(max.Value);
        }

        return Task.FromResult(enumerable.ToList().AsEnumerable());
    }
}
