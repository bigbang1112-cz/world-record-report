using Discord.Webhook;
using Microsoft.Extensions.Logging;
using Discord;
using BigBang1112.WorldRecordReportLib.Repos;

namespace BigBang1112.WorldRecordReportLib.Services;

public class DiscordWebhookService : IDiscordWebhookService
{
    private readonly ILogger<DiscordWebhookService> _logger;
    private readonly IDiscordWebhookMessageRepo _messageRepo;

    public DiscordWebhookService(ILogger<DiscordWebhookService> logger, IDiscordWebhookMessageRepo messageRepo)
    {
        _logger = logger;
        _messageRepo = messageRepo;
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
        
        await _messageRepo.AddAsync(msg, cancellationToken);

        return msg;
    }

    public async Task ModifyMessageAsync(DiscordWebhookMessageModel msg,
                                         string? text = null,
                                         IEnumerable<Embed>? embeds = null,
                                         bool ignoreDisabledState = false,
                                         CancellationToken cancellationToken = default)
    {
        var webhookClient = CreateWebhookClientOrDisable(msg.Webhook, ignoreDisabledState);
        
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

    private DiscordWebhookClient? CreateWebhookClientOrDisable(DiscordWebhookModel webhook, bool ignoreDisabledState = false)
    {
        if (!ignoreDisabledState && webhook.Disabled)
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

    private DiscordWebhookClient? CreateWebhookClient(string webhookUrl, out bool isDeleted)
    {
        isDeleted = false;

        try
        {
            return new DiscordWebhookClient(webhookUrl);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "ArgumentException");
        }
        catch (Discord.Net.HttpException ex)
        {
            _logger.LogWarning(ex, "HttpException");
        }
        catch (InvalidOperationException ex)
        {
            // Could not find a webhook with the supplied credentials.
            _logger.LogWarning(ex, "Could not find a webhook with the supplied credentials. Disabling the webhook automatically.");

            isDeleted = true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unknown exception when validating Discord webhook URL.");
        }

        return null;
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

    public bool TestWebhook(string webhookUrl)
    {
        using var client = CreateWebhookClient(webhookUrl, out _);
        return client is not null;
    }
}
