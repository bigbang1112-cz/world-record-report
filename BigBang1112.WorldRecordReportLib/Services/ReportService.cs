using System.Globalization;
using BigBang1112.WorldRecordReportLib.Enums;
using BigBang1112.WorldRecordReportLib.Models;
using BigBang1112.WorldRecordReportLib.Models.ReportScopes;
using TmEssentials;

namespace BigBang1112.WorldRecordReportLib.Services;

public class ReportService
{
    private readonly IWrUnitOfWork _wrUnitOfWork;
    private readonly IDiscordWebhookService _discordWebhookService;

    public const string LogoIconUrl = "https://bigbang1112.cz/assets/images/logo_small.png";

    public ReportService(IWrUnitOfWork wrUnitOfWork, IDiscordWebhookService discordWebhookService)
    {
        _wrUnitOfWork = wrUnitOfWork;
        _discordWebhookService = discordWebhookService;
    }

    public async Task ReportWorldRecordAsync(WorldRecordModel wr, string scope, CancellationToken cancellationToken)
    {
        var embed = GetDefaultEmbed_NewWorldRecord(wr);
        
        await ReportToAllScopedWebhooksAsync(embed, scope, cancellationToken);
    }

    public async Task ReportDifferencesAsync(LeaderboardChangesRich<Guid> changes, MapModel map, string scope, CancellationToken cancellationToken)
    {
        var lines = CreateLeaderboardChangesStringsForDiscord(changes);

        if (!lines.Any())
        {
            return;
        }

        var embed = new Discord.EmbedBuilder()
            .WithDescription(string.Join('\n', lines))
            .AddField("Leaderboard changes within Top 10", $"[{map.GetHumanizedDeformattedName()}]({map.GetInfoUrl()})")
            .WithColor(new Discord.Color(
                map.Environment.Color[0],
                map.Environment.Color[1],
                map.Environment.Color[2]))
            .Build();

        await ReportToAllScopedWebhooksAsync(embed, scope, cancellationToken);
    }

    private static IEnumerable<string> CreateLeaderboardChangesStringsForDiscord(LeaderboardChangesRich<Guid> changes)
    {
        var dict = new SortedDictionary<int, string>();

        var newRecords = changes.NewRecords.Where(x => x.Rank <= 10).OrderBy(x => x.Time);
        var improvedRecords = changes.ImprovedRecords.Where(x => x.Item1.Rank <= 10).OrderBy(x => x.Item1.Time);

        foreach (var record in newRecords)
        {
            dict.Add(record.Rank.GetValueOrDefault(), $"` {record.Rank:00} ` ` {record.Time} ` by **[{record.DisplayName}](https://trackmania.io/#/player/{record.PlayerId})**");
        }
        
        foreach (var improvedRecord in improvedRecords)
        {
            var (currentRecord, previousRecord) = improvedRecord;

            var delta = (currentRecord.Time - previousRecord.Time).TotalSeconds.ToString("0.000");

            var bracket = previousRecord.Rank is null
                ? $"` {delta} `, from ` {previousRecord.Time} `"
                : $"` {delta} `, from ` {previousRecord.Rank:00} ` ` {previousRecord.Time} `";

            dict.Add(currentRecord.Rank.GetValueOrDefault(), $"` {currentRecord.Rank:00} ` ` {currentRecord.Time} ` ({bracket}) by **[{currentRecord.DisplayName}](https://trackmania.io/#/player/{currentRecord.PlayerId})**");
        }

        foreach (var record in changes.RemovedRecords)
        {
            dict.Add(record.Rank.GetValueOrDefault(), $"` {record.Rank:00} ` ` {record.Time} ` by **[{record.DisplayName}](https://trackmania.io/#/player/{record.PlayerId})**");
        }

        foreach (var item in dict)
        {
            yield return item.Value;
        }
    }

    private async Task ReportToAllScopedWebhooksAsync(Discord.Embed embed, string scope, CancellationToken cancellationToken)
    {
        var scopePath = scope.Split(':');

        foreach (var webhook in await _wrUnitOfWork.DiscordWebhooks.GetAllAsync(cancellationToken))
        {
            if (!WebhookShouldReport(webhook, scopePath))
            {
                continue;
            }

            await _discordWebhookService.SendMessageAsync(webhook, snowflake => new DiscordWebhookMessageModel
            {
                MessageId = snowflake,
                Report = null,
                SentOn = DateTime.UtcNow,
                ModifiedOn = DateTime.UtcNow,
                Webhook = webhook
            }, embeds: embed.Yield(), cancellationToken: cancellationToken);
        }
    }

    private static bool WebhookShouldReport(DiscordWebhookModel webhook, string[] scopePath)
    {
        webhook.Scope = new()
        {
            TM2020 = new()
            {
                Official = new()
                {
                    All = true
                }
            }
        };

        if (webhook.Scope is null)
        {
            return true;
        }

        var scopeObjLayer = (ReportScope)webhook.Scope;
        var scopeTypeLayer = webhook.Scope.GetType();

        foreach (var scope in scopePath)
        {
            var prop = scopeTypeLayer.GetProperty(scope);
            
            if (prop is null)
            {
                return false;
            }

            scopeObjLayer = prop.GetValue(scopeObjLayer) as ReportScope;

            if (scopeObjLayer is null)
            {
                return false;
            }

            if (scopeObjLayer.All)
            {
                return true;
            }

            scopeTypeLayer = prop.PropertyType;
        }

        return true;
    }

    public static Discord.Embed GetDefaultEmbed_NewWorldRecord(WorldRecordModel wr)
    {
        var map = wr.Map;

        var isTMUF = (Game)map.Game.Id == Game.TMUF;

        var time = wr.TimeInt32.ToString(useHundredths: isTMUF);

        if (wr.PreviousWorldRecord is not null)
        {
            var delta = new TimeInt32(wr.Time - wr.PreviousWorldRecord.Time).TotalSeconds
                .ToString(isTMUF ? "0.00" : "0.000", CultureInfo.InvariantCulture);

            time += $" ({delta})";
        }

        var nickname = FilterOutNickname(
            nickname: wr.GetPlayerNicknameDeformatted(),
            loginIfFilteredOut: wr.GetPlayerLogin());

        return new Discord.EmbedBuilder()
            .WithTitle("New world record!")
            .WithFooter("Powered by wr.bigbang1112.cz", LogoIconUrl)
            .WithTimestamp(DateTime.SpecifyKind(wr.DrivenOn, DateTimeKind.Utc))
            .WithThumbnailUrl(map.GetThumbnailUrl())
            .WithColor(new Discord.Color(
                map.Environment.Color[0],
                map.Environment.Color[1],
                map.Environment.Color[2]))
            .AddField("Map", $"[{map.DeformattedName}]({map.GetInfoUrl()})", true)
            .AddField("Time", time, true)
            .AddField("By", nickname, true)
            .Build();
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
        {
            if (nickname.Contains(nick, StringComparison.OrdinalIgnoreCase))
            {
                return $"{nickname} ({loginIfFilteredOut})";
            }
        }
        
        return nickname;
    }
}
