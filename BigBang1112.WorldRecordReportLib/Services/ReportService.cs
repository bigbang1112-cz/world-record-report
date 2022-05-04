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

    public async Task ReportWorldRecordAsync(WorldRecordModel wr, string scope, CancellationToken cancellationToken = default)
    {
        CreateDiscordEmbeds_NewWorldRecord(wr, out var webhookEmbed, out var botEmbed);

        var report = new ReportModel
        {
            Guid = Guid.NewGuid(),
            HappenedOn = DateTime.UtcNow,
            WorldRecord = wr,
            Type = ReportModel.EType.NewWorldRecord
        };

        await _wrUnitOfWork.Reports.AddAsync(report, cancellationToken);

        var webhookEmbeds = new List<Discord.Embed> { webhookEmbed };
        var botEmbeds = new List<Discord.Embed> { botEmbed };

        if (wr.Unverified)
        {
            var lbManialinkEmbed = new Discord.EmbedBuilder()
                .WithFooter("This report was sent early using the Leaderboards manialink and will be verified within an hour.")
                .Build();

            webhookEmbeds.Add(lbManialinkEmbed);
            botEmbeds.Add(lbManialinkEmbed);
        }

        await ReportToAllScopedWebhooksAsync(report, webhookEmbeds, scope, cancellationToken);
    }

    public async Task ReportRemovedWorldRecordsAsync(WorldRecordModel wr,
                                                     IEnumerable<WorldRecordModel> removedWrs,
                                                     string scope,
                                                     CancellationToken cancellationToken = default)
    {
        var embed = GetDefaultEmbed_RemovedWorldRecord(wr, removedWrs);

        var report = new ReportModel
        {
            Guid = Guid.NewGuid(),
            HappenedOn = DateTime.UtcNow,
            WorldRecord = wr,
            RemovedWorldRecord = removedWrs.First(),
            Type = ReportModel.EType.RemovedWorldRecord
        };

        await _wrUnitOfWork.Reports.AddAsync(report, cancellationToken);

        await ReportToAllScopedWebhooksAsync(report, embed.Yield(), scope, cancellationToken);
    }

    public async Task ReportDifferencesAsync<TPlayerId>(LeaderboardChangesRich<TPlayerId> changes,
                                                        MapModel map,
                                                        string scope,
                                                        CancellationToken cancellationToken = default) where TPlayerId : notnull
    {
        var lines = CreateLeaderboardChangesStringsForDiscord(changes);

        if (!lines.Any())
        {
            return;
        }

        var embedBuilder = new Discord.EmbedBuilder()
            .WithTitle(map.GetHumanizedDeformattedName())
            .WithUrl(map.GetInfoUrl())
            .WithDescription(string.Join('\n', lines))
            .WithFooter("Top 10 has changed! | Powered by wr.bigbang1112.cz", LogoIconUrl)
            .WithColor(new Discord.Color(
                map.Environment.Color[0],
                map.Environment.Color[1],
                map.Environment.Color[2]))
            .WithCurrentTimestamp();

        var embedWebhook = embedBuilder.Build();
        var embedBot = embedBuilder.WithFooter("", UrlConsts.Favicon).Build();
        
        var report = new ReportModel
        {
            Guid = Guid.NewGuid(),
            HappenedOn = DateTime.UtcNow,
            Type = ReportModel.EType.LeaderboardDifferences
        };

        await _wrUnitOfWork.Reports.AddAsync(report, cancellationToken);

        await ReportToAllScopedWebhooksAsync(report, embedWebhook.Yield(), scope, cancellationToken);
    }

    private static IEnumerable<string> CreateLeaderboardChangesStringsForDiscord<TPlayerId>(LeaderboardChangesRich<TPlayerId> changes) where TPlayerId : notnull
    {
        var dict = new SortedDictionary<int, string>();

        var newRecords = changes.NewRecords.Where(x => x.Rank <= 10).OrderBy(x => x.Time);
        var improvedRecords = changes.ImprovedRecords.Where(x => x.Item1.Rank <= 10).OrderBy(x => x.Item1.Time);

        foreach (var record in newRecords)
        {
            dict.Add(record.Rank.GetValueOrDefault(), $"` {record.Rank:00} ` ` {record.Time} ` by **{record.GetDisplayNameMdLink()}**");
        }
        
        foreach (var improvedRecord in improvedRecords)
        {
            var (currentRecord, previousRecord) = improvedRecord;

            var delta = (currentRecord.Time - previousRecord.Time).TotalSeconds.ToString("0.000");

            var bracket = previousRecord.Rank is null
                ? $"` {delta} `, from ` {previousRecord.Time} `"
                : $"` {delta} `, from ` {previousRecord.Rank:00} ` ` {previousRecord.Time} `";

            dict.Add(currentRecord.Rank.GetValueOrDefault(), $"` {currentRecord.Rank:00} ` ` {currentRecord.Time} ` ({bracket}) by **{currentRecord.GetDisplayNameMdLink()}**");
        }

        foreach (var record in changes.RemovedRecords)
        {
            dict.Add(record.Rank.GetValueOrDefault(), $"` {record.Rank:00} ` ` {record.Time} ` by **{record.GetDisplayNameMdLink()}** was **removed**");
        }

        foreach (var item in dict)
        {
            yield return item.Value;
        }
    }

    private async Task ReportToAllScopedWebhooksAsync(ReportModel report, IEnumerable<Discord.Embed> embeds, string scope, CancellationToken cancellationToken)
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
                Report = report,
                SentOn = DateTime.UtcNow,
                ModifiedOn = DateTime.UtcNow,
                Webhook = webhook
            }, embeds: embeds, cancellationToken: cancellationToken);
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
            },
            Tmx = new()
            {
                Official = new()
                {
                    All = true
                }
            },
            TM2 = new()
            {
                Nadeo = new()
                {
                    All = true
                }
            },
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

    public static Discord.EmbedBuilder GetDefaultEmbedBuilder_NewWorldRecord(WorldRecordModel wr)
    {
        var map = wr.Map;

        var isTMUF = map.Game.Id == (int)Game.TMUF;
        var isStunts = map.IsStuntsMode();

        var score = $"` {(isStunts ? wr.Time.ToString() : wr.TimeInt32.ToString(useHundredths: isTMUF))} `";

        if (wr.PreviousWorldRecord is not null)
        {
            if (isStunts)
            {
                score += $" ` +{wr.Time - wr.PreviousWorldRecord.Time} `";
            }
            else
            {
                var delta = new TimeInt32(wr.Time - wr.PreviousWorldRecord.Time).TotalSeconds
                    .ToString(isTMUF ? "0.00" : "0.000", CultureInfo.InvariantCulture);

                score += $" ` {delta} `";
            }
        }

        var nickname = FilterOutNickname(
            nickname: wr.GetPlayerNicknameMdLink(),
            loginIfFilteredOut: wr.GetPlayerLogin());

        return new Discord.EmbedBuilder()
            .WithTitle("New world record!")
            .WithFooter("Powered by wr.bigbang1112.cz", LogoIconUrl)
            .WithTimestamp(DateTime.SpecifyKind(wr.DrivenOn, DateTimeKind.Utc))
            .WithColor(new Discord.Color(
                map.Environment.Color[0],
                map.Environment.Color[1],
                map.Environment.Color[2]))
            .WithThumbnailUrl(map.GetThumbnailUrl())
            .AddField("Map", map.GetMdLink(), true)
            .AddField(isStunts ? "Score" : "Time", score, true)
            .AddField("By", $"**{nickname}**", true);
    }

    public static Discord.Embed GetDefaultEmbed_RemovedWorldRecord(WorldRecordModel? currentWr, IEnumerable<WorldRecordModel> removedWrs)
    {
        if (!removedWrs.Any())
        {
            throw new Exception("Removed WRs need to be at least 1.");
        }

        var previousWr = removedWrs.First();
        var map = previousWr.Map;
        var time = previousWr.TimeInt32.ToString();

        var builder = new Discord.EmbedBuilder()
            .WithTitle("Removed world record")
            .WithFooter("Powered by wr.bigbang1112.cz", LogoIconUrl)
            .WithColor(new Discord.Color(
                map.Environment.Color[0],
                map.Environment.Color[1],
                map.Environment.Color[2]))
            .WithThumbnailUrl(map.GetThumbnailUrl())
            .AddField("Map", $"[{map.DeformattedName}]({map.GetInfoUrl()})", true)
            .AddField("Time", time, true)
            .AddField("By", previousWr.GetPlayerNicknameMdLink(), true);

        if (currentWr is not null)
        {
            var prevTime = currentWr.TimeInt32.ToString();
            var prevNickname = currentWr.GetPlayerNicknameMdLink();

            builder = builder
                .AddField("New time", prevTime, true)
                .AddField("Now by", prevNickname, true);
        }

        return builder.Build();
    }

    public async Task UpdateWorldRecordReportAsync(ReportModel report, CancellationToken cancellationToken = default)
    {
        if (report.WorldRecord is null)
        {
            return;
        }

        CreateDiscordEmbeds_NewWorldRecord(report.WorldRecord, out var webhookEmbed, out var botEmbed);

        foreach (var msg in report.DiscordWebhookMessages)
        {
            await _discordWebhookService.ModifyMessageAsync(msg, embeds: webhookEmbed.Yield(), cancellationToken: cancellationToken);
        }
    }

    private static void CreateDiscordEmbeds_NewWorldRecord(WorldRecordModel wr, out Discord.Embed webhookEmbed, out Discord.Embed botEmbed)
    {
        var embedBuilder = GetDefaultEmbedBuilder_NewWorldRecord(wr);

        webhookEmbed = embedBuilder.Build();
        botEmbed = embedBuilder.WithFooter(wr.Guid.ToString(), UrlConsts.Favicon).Build();
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

    public async Task RemoveWorldRecordReportAsync(WorldRecordModel wr)
    {
        var report = await _wrUnitOfWork.Reports.GetByWorldRecordAsync(wr);

        if (report is null)
        {
            return;
        }
    }
}
