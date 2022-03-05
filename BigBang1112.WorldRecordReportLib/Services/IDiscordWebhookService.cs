using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Models;
using Discord;
using Discord.Webhook;

namespace BigBang1112.WorldRecordReportLib.Services;

public interface IDiscordWebhookService
{
    Embed GetDefaultEmbed_NewStuntsWorldRecord(WorldRecordModel wr);
    Embed GetDefaultEmbed_NewWorldRecord(WorldRecordModel wr);
    Embed GetDefaultEmbed_RemovedWorldRecord(RemovedWorldRecord removedWr);
    Task<DiscordWebhookMessageModel?> SendMessageAsync(DiscordWebhookModel webhook, Func<ulong, DiscordWebhookMessageModel>? message = null, string? text = null, IEnumerable<Embed>? embeds = null);
    DiscordWebhookClient? ValidateWebhookUrl(string webhookUrl);
    Task DeleteMessageAsync(DiscordWebhookMessageModel msg);
}
