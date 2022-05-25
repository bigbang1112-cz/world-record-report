using BigBang1112.WorldRecordReportLib.Models;
using Discord.Webhook;
using System.Globalization;
using TmEssentials;
using BigBang1112.WorldRecordReportLib.Repos;
using Microsoft.Extensions.Logging;
using Discord;

namespace BigBang1112.WorldRecordReportLib.Services;

public class DiscordWebhookService : IDiscordWebhookService
{
    private readonly ILogger<DiscordWebhookService> _logger;
    private readonly IWrUnitOfWork _wrUnitOfWork;

    public const string LogoIconUrl = "https://bigbang1112.cz/assets/images/logo_small.png";

    public DiscordWebhookService(ILogger<DiscordWebhookService> logger, IWrUnitOfWork wrUnitOfWork)
    {
        _logger = logger;
        _wrUnitOfWork = wrUnitOfWork;
    }

    public async Task<DiscordWebhookMessageModel?> SendMessageAsync(DiscordWebhookModel webhook,
                                                                    Func<ulong, DiscordWebhookMessageModel>? message = null,
                                                                    string? text = null,
                                                                    IEnumerable<Embed>? embeds = null,
                                                                    CancellationToken cancellationToken = default)
    {
        var webhookClient = CreateWebhookClientOrDisable(webhook);

        if (webhookClient is null)
        {
            return null;
        }

        var msgId = await webhookClient.SendMessageAsync(text, embeds: embeds);

        if (message is null)
        {
            return null;
        }

        var msg = message.Invoke(msgId);

        await _wrUnitOfWork.DiscordWebhookMessages.AddAsync(msg, cancellationToken);

        return msg;
    }

    public async Task ModifyMessageAsync(DiscordWebhookMessageModel msg,
                                         string? text = null,
                                         IEnumerable<Embed>? embeds = null,
                                         CancellationToken cancellationToken = default)
    {
        var webhookClient = CreateWebhookClientOrDisable(msg.Webhook);

        if (webhookClient is null)
        {
            return;
        }

        msg.ModifiedOn = DateTime.UtcNow;

        try
        {
            await webhookClient.ModifyMessageAsync(msg.MessageId, x =>
            {
                if (text is not null)
                {
                    x.Content = text;
                }

                if (embeds is not null)
                {
                    x.Embeds = new(embeds);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Message (DB ID: {msgId}) couldn't be modified.", msg.Id);
        }
    }

    private DiscordWebhookClient? CreateWebhookClientOrDisable(DiscordWebhookModel webhook)
    {
        if (webhook.Disabled)
        {
            return null;
        }

        var webhookClient = CreateWebhookClient(webhook.Url, out bool isDeleted);

        if (isDeleted)
        {
            _logger.LogWarning("Webhook {guid} is deleted", webhook.Guid);

            webhook.Disabled = true;
        }

        return webhookClient;
    }

    public DiscordWebhookClient? CreateWebhookClient(string webhookUrl, out bool isDeleted)
    {
        isDeleted = false;

        try
        {
            return new DiscordWebhookClient(webhookUrl);
        }
        catch (ArgumentException)
        {

        }
        catch (Discord.Net.HttpException)
        {

        }
        catch (InvalidOperationException)
        {
            // Could not find a webhook with the supplied credentials.

            isDeleted = true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unknown exception when validating Discord webhook URL.");
        }

        return null;
    }

    private static Discord.EmbedBuilder AddThumbnailAndUrl(Discord.EmbedBuilder builder, MapModel map)
    {
        if (map.TmxAuthor is null)
        {
            // Currently TM2 maps on MX, should be adjusted in the further time

            builder.WithUrl("https://tm.mania.exchange/s/tr/" + map.MxId)
                .WithThumbnailUrl("https://tm.mania-exchange.com/tracks/thumbnail/" + map.MxId);
        }
        else
        {
            var uri = new Uri(map.TmxAuthor.Site.Url);

            switch (map.TmxAuthor.Site.ShortName)
            {
                case NameConsts.TMXSiteUnited:
                case NameConsts.TMXSiteTMNF:
                    builder.WithUrl(new Uri(uri, $"trackshow/{map.MxId}").ToString())
                        .WithThumbnailUrl(new Uri(uri, $"trackshow/{map.MxId}/image/0").ToString());
                    break;
                case NameConsts.TMXSiteNations:
                    builder.WithUrl(new Uri(uri, "main.aspx?action=trackshow&id=" + map.MxId).ToString())
                        .WithThumbnailUrl(new Uri(uri, "getclean.aspx?action=trackscreen&id=" + map.MxId).ToString());
                    break;
                default:
                    throw new Exception();
            }
        }

        return builder;
    }

    private static string FilterOutNickname(string nickname, string loginIfFilteredOut)
    {
        var nicks = new string[]
        {
            "riolu",
            "r¡olu",
            "techno",
            "hylis"
        };

        foreach (var nick in nicks)
            if (nickname.Contains(nick, StringComparison.OrdinalIgnoreCase))
                return $"{nickname} ({loginIfFilteredOut})";
        return nickname;
    }

    public async Task DeleteMessageAsync(DiscordWebhookMessageModel msg, CancellationToken cancellationToken = default)
    {
        using var webhookClient = CreateWebhookClientOrDisable(msg.Webhook);

        if (webhookClient is null)
        {
            return;
        }

        try
        {
            await webhookClient.DeleteMessageAsync(msg.MessageId);
            msg.RemovedOfficially = true;
        }
        catch (Discord.Net.HttpException httpEx)
        {
            if (httpEx.HttpCode == System.Net.HttpStatusCode.NotFound
            && !msg.RemovedOfficially)
            {
                msg.RemovedByUser = true;
            }
        }
    }
}
