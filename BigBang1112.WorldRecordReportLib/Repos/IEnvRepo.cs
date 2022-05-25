using BigBang1112.WorldRecordReportLib.Enums;

namespace BigBang1112.WorldRecordReportLib.Repos;

public interface IEnvRepo : IEnumRepo<EnvModel, Env>
{
    Task<IEnumerable<string>> GetAllNamesLikeAsync(string value, int limit = DiscordConsts.OptionLimit, CancellationToken cancellationToken = default);
}
