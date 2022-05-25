using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Models;
using Discord;
using Discord.Webhook;

namespace BigBang1112.WorldRecordReportLib.Services;

public interface IDiscordWebhookService
{
    Task<DiscordWebhookMessageModel?> SendMessageAsync(DiscordWebhookModel webhook, Func<ulong, DiscordWebhookMessageModel>? message = null, string? text = null, IEnumerable<Embed>? embeds = null, CancellationToken cancellationToken = default);
    DiscordWebhookClient? CreateWebhookClient(string webhookUrl, out bool isDeleted);
    Task DeleteMessageAsync(DiscordWebhookMessageModel msg, CancellationToken cancellationToken = default);
    Task ModifyMessageAsync(DiscordWebhookMessageModel msg, string? text = null, IEnumerable<Embed>? embeds = null, CancellationToken cancellationToken = default);
}
