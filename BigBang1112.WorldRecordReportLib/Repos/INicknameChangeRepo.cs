namespace BigBang1112.WorldRecordReportLib.Repos;

public interface INicknameChangeRepo : IRepo<NicknameChangeModel>
{
    Task<NicknameChangeModel?> GetLatestByLoginAsync(LoginModel loginModel, CancellationToken cancellationToken = default);
}
