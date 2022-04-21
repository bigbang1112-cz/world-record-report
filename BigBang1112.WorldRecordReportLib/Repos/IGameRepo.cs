using BigBang1112.WorldRecordReportLib.Enums;

namespace BigBang1112.WorldRecordReportLib.Repos;

public interface IGameRepo : IEnumRepo<GameModel, Game>
{
    Task<IEnumerable<string>> GetAllNamesLikeAsync(string value, int? max = null, CancellationToken cancellationToken = default);
    Task<GameModel?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
}
