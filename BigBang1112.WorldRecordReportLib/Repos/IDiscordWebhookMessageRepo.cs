namespace BigBang1112.WorldRecordReportLib.Repos;

public interface IDiscordWebhookMessageRepo : IRepo<DiscordWebhookMessageModel>
{
    Task<IEnumerable<DiscordWebhookMessageModel>> GetAllByReportAsync(ReportModel report, CancellationToken cancellationToken = default);
}
