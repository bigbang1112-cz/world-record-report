namespace BigBang1112.WorldRecordReportLib.Repos;

public interface INicknameChangeRepo : IRepo<NicknameChangeModel>
{
    Task<IEnumerable<NicknameChangeModel>> GetHistoryAsync(LoginModel loginModel, CancellationToken cancellationToken = default);
    Task<NicknameChangeModel?> GetLatestByLoginAsync(LoginModel loginModel, CancellationToken cancellationToken = default);
}
