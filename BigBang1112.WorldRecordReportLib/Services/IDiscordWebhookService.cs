using Discord;

namespace BigBang1112.WorldRecordReportLib.Services;

public interface IDiscordWebhookService
{
    bool ValidateWebhook(string webhookUrl);
    Task<DiscordWebhookMessageModel?> SendMessageAsync(DiscordWebhookModel webhook, Func<ulong, DiscordWebhookMessageModel>? message = null, string? text = null, IEnumerable<Embed>? embeds = null, CancellationToken cancellationToken = default);
    Task DeleteMessageAsync(DiscordWebhookMessageModel msg, CancellationToken cancellationToken = default);
    Task ModifyMessageAsync(DiscordWebhookMessageModel msg, string? text = null, IEnumerable<Embed>? embeds = null, bool ignoreDisabledState = false, CancellationToken cancellationToken = default);
}
