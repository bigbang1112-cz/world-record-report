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
                                                        int maxRank = 10,
                                                        CancellationToken cancellationToken = default) where TPlayerId : notnull
    {
        var lines = CreateLeaderboardChangesStringsForDiscord(map, changes, maxRank);

        if (!lines.Any())
        {
            return;
        }

        var latestChange = GetLatestChange(changes);

        var embedBuilder = new Discord.EmbedBuilder()
            .WithTitle(map.GetHumanizedDeformattedName())
            .WithUrl(map.GetInfoUrl())
            .WithDescription(string.Join('\n', lines))
            .WithFooter("Powered by wr.bigbang1112.cz", LogoIconUrl)
            .WithColor(new Discord.Color(
                map.Environment.Color[0],
                map.Environment.Color[1],
                map.Environment.Color[2]))
            .WithTimestamp(latestChange);

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

    private static DateTime GetLatestChange<TPlayerId>(LeaderboardChangesRich<TPlayerId> changes) where TPlayerId : notnull
    {
        var dateTime = DateTime.MinValue;

        foreach (var record in changes.NewRecords)
        {
            switch (record)
            {
                case TmxReplay tmxReplay: if (tmxReplay.ReplayAt > dateTime) dateTime = tmxReplay.ReplayAt; break;
                case TM2Record: return DateTime.UtcNow; // TODO: add driven at to TM2Record
                case TM2020Record tm2020Record: if (tm2020Record.Timestamp > dateTime) dateTime = tm2020Record.Timestamp; break;
            }
        }

        foreach (var (newRecord, _) in changes.ImprovedRecords)
        {
            switch (newRecord)
            {
                case TmxReplay tmxReplay: if (tmxReplay.ReplayAt > dateTime) dateTime = tmxReplay.ReplayAt; break;
                case TM2Record: return DateTime.UtcNow; // TODO: add driven at to TM2Record
                case TM2020Record tm2020Record: if (tm2020Record.Timestamp > dateTime) dateTime = tm2020Record.Timestamp; break;
            }
        }

        foreach (var record in changes.RemovedRecords)
        {
            switch (record)
            {
                case TmxReplay tmxReplay: if (tmxReplay.ReplayAt > dateTime) dateTime = tmxReplay.ReplayAt; break;
                case TM2Record: return DateTime.UtcNow; // TODO: add driven at to TM2Record
                case TM2020Record tm2020Record: if (tm2020Record.Timestamp > dateTime) dateTime = tm2020Record.Timestamp; break;
            }
        }

        if (dateTime == DateTime.MinValue)
        {
            return DateTime.UtcNow;
        }

        return dateTime;
    }

    private static IEnumerable<string> CreateLeaderboardChangesStringsForDiscord<TPlayerId>(
        MapModel map,
        LeaderboardChangesRich<TPlayerId> changes,
        int maxRank) where TPlayerId : notnull
    {
        var isTMUF = map.Game.IsTMUF();

        var dict = new SortedDictionary<int, string>();

        var newRecords = changes.NewRecords
            .Where(x => x.Rank <= maxRank)
            .OrderBy(x => x.Time)
            .ToList();

        var improvedRecords = changes.ImprovedRecords
            .Where(x => x.Item1.Rank <= maxRank)
            .OrderBy(x => x.Item1.Time)
            .ToList();

        var includeTimestamp = newRecords.Count > 1 || improvedRecords.Count > 1
            || (newRecords.Count > 0 && improvedRecords.Count > 0);

        foreach (var record in newRecords)
        {
            var timestamp = GetTimestamp(record);
            var timestampBracket = includeTimestamp && timestamp.HasValue ? $" ({timestamp.Value.ToTimestampTag(Discord.TimestampTagStyles.ShortTime)})" : "";

            dict.Add(record.Rank.GetValueOrDefault(), $"` {record.Rank:00} ` ` {record.Time.ToString(useHundredths: isTMUF)} ` by **{GetDisplayNameMdLink(map, record)}**{timestampBracket}");
        }

        foreach (var (currentRecord, previousRecord) in improvedRecords)
        {
            var delta = (currentRecord.Time - previousRecord.Time).TotalSeconds.ToString(isTMUF ? "0.00" : "0.000");

            var bracket = previousRecord.Rank is null
                ? $"` {delta} `, from ` {previousRecord.Time} `"
                : $"` {delta} `, from ` {previousRecord.Rank:00} ` ` {previousRecord.Time.ToString(useHundredths: isTMUF)} `";

            var timestamp = GetTimestamp(currentRecord);
            var timestampBracket = includeTimestamp && timestamp.HasValue ? $" ({timestamp.Value.ToTimestampTag(Discord.TimestampTagStyles.ShortTime)})" : "";

            dict.Add(currentRecord.Rank.GetValueOrDefault(), $"` {currentRecord.Rank:00} ` ` {currentRecord.Time.ToString(useHundredths: isTMUF)} ` ({bracket}) by **{GetDisplayNameMdLink(map, currentRecord)}**{timestampBracket}");
        }

        foreach (var record in changes.RemovedRecords)
        {
            dict.Add(record.Rank.GetValueOrDefault(), $"` {record.Rank:00} ` ` {record.Time.ToString(useHundredths: isTMUF)} ` by **{GetDisplayNameMdLink(map, record)}** was **removed**");
        }

        foreach (var item in dict)
        {
            yield return item.Value;
        }
    }

    private static DateTime? GetTimestamp<TPlayerId>(IRecord<TPlayerId> record) where TPlayerId : notnull => record switch
    {
        TmxReplay tmxReplay => tmxReplay.ReplayAt,
        //TM2Record tm2Record => tm2Record.DrivenAt,
        TM2020Record tm2020Record => tm2020Record.Timestamp,
        _ => null
    };

    private static string GetDisplayNameMdLink<TPlayerId>(MapModel map, IRecord<TPlayerId> record) where TPlayerId : notnull
    {
        return record is TmxReplay tmxReplay && map.TmxAuthor is not null
            ? tmxReplay.GetDisplayNameMdLink((TmxSite)map.TmxAuthor.Site.Id)
            : record.GetDisplayNameMdLink();
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
                    WR = new()
                }
            },
            TMUF = new()
            {
                TMX = new()
                {
                    Official = new()
                    {
                        Changes = new()
                    }
                }
            },
            TM2 = new()
            {
                Nadeo = new()
                {
                    Changes = new() { Param = new[] {"TMLagoon@nadeo", "TMValley@nadeo"}}
                }
            },
        };

        if (webhook.Filter is not null)
        {
            return false;
        }

        if (webhook.Scope is null)
        {
            return true;
        }

        var scopeObjLayer = (ReportScope)webhook.Scope;
        var scopeTypeLayer = webhook.Scope.GetType();

        // Loops through the scope triggered by the report itself (not the user scope preference)
        // 'scope' is taken as each child in scope sequence
        foreach (var scope in scopePath)
        {
            // This check is about making sure that the current sub-scope is static or varies
            // It cannot be at the end because varying scopes have less strict rules
            if (scopeObjLayer is ReportScopeWithParam scopeWithParam)
            {
                // Parameter that doesn't try to tell anything is taken as valid parent scope
                // Child scopes aren't taken into consideration as they are not allowed with Param type of scope
                if (scopeWithParam.Param is null)
                {
                    return true;
                }

                foreach (var param in scopeWithParam.Param)
                {
                    // That the parameter just starts with the wanted scope is enough, as Param scope cannot have child scopes
                    if (scope.StartsWith(param))
                    {
                        return true;
                    }
                }
            }

            var prop = scopeTypeLayer.GetProperty(scope);
            
            if (prop is null)
            {
                return false;
            }

            scopeObjLayer = prop.GetValue(scopeObjLayer) as ReportScope;

            // If report scope is simply not present
            if (scopeObjLayer is null)
            {
                // Report if it's at least the root scope
                return scopeObjLayer is not ReportScopeSet;
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
        var time = $"` {previousWr.TimeInt32} `";

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
            .AddField("By", $"**{previousWr.GetPlayerNicknameMdLink()}**", true);

        if (currentWr is not null)
        {
            var prevTime = $"` {currentWr.TimeInt32} `";
            var prevNickname = $"**{currentWr.GetPlayerNicknameMdLink()}**";

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
