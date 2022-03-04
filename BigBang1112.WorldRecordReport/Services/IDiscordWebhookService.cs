using BigBang1112.WorldRecordReport.Models;
using BigBang1112.WorldRecordReport.Models.Db;
using Discord;
using Discord.Webhook;

namespace BigBang1112.WorldRecordReport.Services;

public interface IDiscordWebhookService
{
    Embed GetDefaultEmbed_NewStuntsWorldRecord(WorldRecordModel wr);
    Embed GetDefaultEmbed_NewWorldRecord(WorldRecordModel wr);
    Embed GetDefaultEmbed_RemovedWorldRecord(RemovedWorldRecord removedWr);
    Task<DiscordWebhookMessageModel?> SendMessageAsync(DiscordWebhookModel webhook, Func<ulong, DiscordWebhookMessageModel>? message = null, string? text = null, IEnumerable<Embed>? embeds = null);
    DiscordWebhookClient? ValidateWebhookUrl(string webhookUrl);
    Task DeleteMessageAsync(DiscordWebhookMessageModel msg);
}
