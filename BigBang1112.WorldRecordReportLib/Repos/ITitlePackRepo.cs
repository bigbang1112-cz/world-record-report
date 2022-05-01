namespace BigBang1112.WorldRecordReportLib.Repos;

public interface ITitlePackRepo : IRepo<TitlePackModel>
{
    Task<IEnumerable<string>> GetAllUidsLikeAsync(string value, int limit = DiscordConsts.OptionLimit, CancellationToken cancellationToken = default);
    Task<TitlePackModel?> GetByFullUidAsync(string titleId, CancellationToken cancellationToken = default);
}
