using BigBang1112.WorldRecordReportLib.Data;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Models;
using Discord.Webhook;
using System.Globalization;
using TmEssentials;
using BigBang1112.WorldRecordReportLib.Repos;
using Microsoft.Extensions.Logging;

namespace BigBang1112.WorldRecordReportLib.Services;

public class DiscordWebhookService : IDiscordWebhookService
{
    private readonly ILogger<DiscordWebhookService> _logger;
    private readonly IWrRepo _repo;

    public const string LogoIconUrl = "https://bigbang1112.cz/assets/images/logo_small.png";

    public DiscordWebhookService(ILogger<DiscordWebhookService> logger, IWrRepo repo)
    {
        _logger = logger;
        _repo = repo;
    }

    public async Task<DiscordWebhookMessageModel?> SendMessageAsync(DiscordWebhookModel webhook, Func<ulong, DiscordWebhookMessageModel>? message = null, string? text = null, IEnumerable<Discord.Embed>? embeds = null)
    {
        using var webhookClient = CreateWebhookClient(webhook.Url);

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

        await _repo.AddDiscordWebhookMessageAsync(msg);

        return msg;
    }

    public DiscordWebhookClient? CreateWebhookClient(string webhookUrl)
    {
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
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unknown exception when validating Discord webhook URL.");
        }

        return null;
    }

    public Discord.Embed GetDefaultEmbed_NewWorldRecord(WorldRecordModel wr)
    {
        var map = wr.Map;

        var deltaFormat = map.Game.Name == NameConsts.GameTMUFName ? "0.00" : "0.000";

        var time = wr.TimeInt32.ToString(map.Game.Name == NameConsts.GameTMUFName);

        if (wr.PreviousWorldRecord is not null)
        {
            var delta = new TimeInt32(wr.Time - wr.PreviousWorldRecord.Time).TotalSeconds.ToString(deltaFormat, CultureInfo.InvariantCulture);
            time += $" ({delta})";
        }

        var nickname = FilterOutNickname(
            nickname: wr.GetPlayerNicknameDeformatted(),
            loginIfFilteredOut: wr.GetPlayerLogin());

        var builder = new Discord.EmbedBuilder()
            .WithTitle("New world record!")
            .WithFooter("Powered by wr.bigbang1112.cz", LogoIconUrl)
            .WithTimestamp(DateTime.SpecifyKind(wr.DrivenOn, DateTimeKind.Utc))
            .WithColor(new Discord.Color(
                map.Environment.Color[0],
                map.Environment.Color[1],
                map.Environment.Color[2]))
            .AddField("Map", TextFormatter.Deformat(map.Name), true)
            .AddField("Time", time, true)
            .AddField("By", nickname, true);

        AddThumbnailAndUrl(builder, map);

        return builder.Build();
    }

    public Discord.Embed GetDefaultEmbed_NewStuntsWorldRecord(WorldRecordModel wr)
    {
        var map = wr.Map;

        var score = wr.Time.ToString();
        if (wr.PreviousWorldRecord is not null)
            score += $" (+{wr.Time - wr.PreviousWorldRecord.Time})";

        var builder = new Discord.EmbedBuilder()
            .WithTitle("New world record!")
            .WithFooter("Powered by wr.bigbang1112.cz", LogoIconUrl)
            .WithTimestamp(DateTime.SpecifyKind(wr.DrivenOn, DateTimeKind.Utc))
            .WithColor(new Discord.Color(
                map.Environment.Color[0],
                map.Environment.Color[1],
                map.Environment.Color[2]))
            .AddField("Map", TextFormatter.Deformat(map.Name), true)
            .AddField("Score", score, true)
            .AddField("By", TextFormatter.Deformat(wr.Player?.Nickname ?? wr.TmxPlayer?.Nickname ?? "[unknown nickname]"), true);

        AddThumbnailAndUrl(builder, map);

        return builder.Build();
    }

    public Discord.Embed GetDefaultEmbed_RemovedWorldRecord(RemovedWorldRecord removedWr)
    {
        var previousWr = removedWr.Previous;
        var map = previousWr.Map;
        var time = previousWr.TimeInt32.ToString();

        var builder = new Discord.EmbedBuilder()
            .WithTitle("Removed world record detected")
            .WithFooter("Powered by wr.bigbang1112.cz", LogoIconUrl)
            .WithColor(new Discord.Color(
                map.Environment.Color[0],
                map.Environment.Color[1],
                map.Environment.Color[2]))
            .AddField("Map", map.DeformattedName, true)
            .AddField("Time", time, true)
            .AddField("By", previousWr.GetPlayerNicknameDeformatted().EscapeDiscord(), true);

        var currentWr = removedWr.Current;

        if (currentWr is not null)
        {
            var prevTime = currentWr.TimeInt32.ToString();
            var prevNickname = currentWr.GetPlayerNicknameDeformatted().EscapeDiscord();

            builder
                .AddField("New time", prevTime, true)
                .AddField("Now by", prevNickname, true);
        }

        AddThumbnailAndUrl(builder, map);

        return builder.Build();
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

    public async Task DeleteMessageAsync(DiscordWebhookMessageModel msg)
    {
        using var webhookClient = CreateWebhookClient(msg.Webhook.Url);

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
