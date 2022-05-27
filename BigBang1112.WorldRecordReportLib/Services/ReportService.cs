﻿using System.Globalization;
using BigBang1112.DiscordBot;
using BigBang1112.DiscordBot.Data;
using BigBang1112.DiscordBot.Models.Db;
using BigBang1112.WorldRecordReportLib.Enums;
using BigBang1112.WorldRecordReportLib.Models;
using BigBang1112.WorldRecordReportLib.Models.ReportScopes;
using BigBang1112.WorldRecordReportLib.TMWR;
using Microsoft.Extensions.Logging;
using TmEssentials;

namespace BigBang1112.WorldRecordReportLib.Services;

public class ReportService
{
    private readonly IWrUnitOfWork _wrUnitOfWork;
    private readonly IDiscordWebhookService _discordWebhookService;
    private readonly IDiscordBotUnitOfWork _discordBotUnitOfWork;
    private readonly TmwrDiscordBotService _tmwrDiscordBotService;
    private readonly ILogger<ReportService> _logger;

    public const string LogoIconUrl = "https://bigbang1112.cz/assets/images/logo_small.png";

    public ReportService(IWrUnitOfWork wrUnitOfWork,
                         IDiscordBotUnitOfWork discordBotUnitOfWork,
                         IDiscordWebhookService discordWebhookService,
                         TmwrDiscordBotService tmwrDiscordBotService,
                         ILogger<ReportService> logger)
    {
        _wrUnitOfWork = wrUnitOfWork;
        _discordWebhookService = discordWebhookService;
        _discordBotUnitOfWork = discordBotUnitOfWork;
        _tmwrDiscordBotService = tmwrDiscordBotService;
        _logger = logger;
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

        var components = new Discord.ComponentBuilder()
            .WithButton("Details", DiscordBotService.CreateCustomId($"wr-{wr.Guid.ToString().Replace('-', '_')}"), Discord.ButtonStyle.Primary)
            .WithButton("Previous", DiscordBotService.CreateCustomId($"wr-{wr.Guid.ToString().Replace('-', '_')}-prev"), Discord.ButtonStyle.Secondary)
            .WithButton("Compare with previous", DiscordBotService.CreateCustomId($"comparewrs-{wr.Guid.ToString().Replace('-', '_')}-prev"), Discord.ButtonStyle.Secondary)
            .WithButton("History", DiscordBotService.CreateCustomId($"historywr-{wr.Map.MapUid}"), Discord.ButtonStyle.Secondary)
            .WithButton("Map info", DiscordBotService.CreateCustomId($"mapinfo-{wr.Map.MapUid}"), Discord.ButtonStyle.Secondary)
            .Build();

        await ReportToAllScopedDiscordBotsAsync(report, botEmbeds, components, scope, cancellationToken);
        await ReportToAllScopedDiscordWebhooksAsync(report, webhookEmbeds, scope, cancellationToken);
    }

    public async Task ReportRemovedWorldRecordsAsync(WorldRecordModel wr,
                                                     IEnumerable<WorldRecordModel> removedWrs,
                                                     string scope,
                                                     CancellationToken cancellationToken = default)
    {
        CreateDiscordEmbeds_RemovedWorldRecord(wr, removedWrs, out var webhookEmbed, out var botEmbed);

        var report = new ReportModel
        {
            Guid = Guid.NewGuid(),
            HappenedOn = DateTime.UtcNow,
            WorldRecord = wr,
            RemovedWorldRecord = removedWrs.First(),
            Type = ReportModel.EType.RemovedWorldRecord
        };

        await _wrUnitOfWork.Reports.AddAsync(report, cancellationToken);

        await ReportToAllScopedDiscordBotsAsync(report, botEmbed.Yield(), components: null, scope, cancellationToken);
        await ReportToAllScopedDiscordWebhooksAsync(report, webhookEmbed.Yield(), scope, cancellationToken);

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
            .WithDescription(string.Join('\n', lines))
            .WithColor(new Discord.Color(
                map.Environment.Color[0],
                map.Environment.Color[1],
                map.Environment.Color[2]));

        /*if (latestChange.HasValue)
        {
            embedBuilder.Timestamp = latestChange;
        }*/

        var embedBot = embedBuilder.Build();
        var embedWebhook = embedBuilder
            .WithFooter("Powered by wr.bigbang1112.cz", LogoIconUrl)
            .Build();

        var report = new ReportModel
        {
            Guid = Guid.NewGuid(),
            HappenedOn = DateTime.UtcNow,
            Type = ReportModel.EType.LeaderboardDifferences
        };

        await _wrUnitOfWork.Reports.AddAsync(report, cancellationToken);

        await ReportToAllScopedDiscordWebhooksAsync(report, embedWebhook.Yield(), scope, cancellationToken);
        await ReportToAllScopedDiscordBotsAsync(report, embedBot.Yield(), null, scope, cancellationToken);
    }

    private static DateTime? GetLatestChange<TPlayerId>(LeaderboardChangesRich<TPlayerId> changes) where TPlayerId : notnull
    {
        var dateTime = DateTime.MinValue;

        foreach (var record in changes.NewRecords)
        {
            switch (record)
            {
                case TmxReplay tmxReplay: if (tmxReplay.ReplayAt > dateTime) dateTime = tmxReplay.ReplayAt; break;
                case TM2Record tm2Record: if (tm2Record.Timestamp.HasValue && tm2Record.Timestamp.Value.UtcDateTime > dateTime) dateTime = tm2Record.Timestamp.Value.UtcDateTime; break;
                case TM2020Record tm2020Record: if (tm2020Record.Timestamp > dateTime) dateTime = tm2020Record.Timestamp; break;
            }
        }

        foreach (var (newRecord, _) in changes.ImprovedRecords)
        {
            switch (newRecord)
            {
                case TmxReplay tmxReplay: if (tmxReplay.ReplayAt > dateTime) dateTime = tmxReplay.ReplayAt; break;
                case TM2Record tm2Record: if (tm2Record.Timestamp.HasValue && tm2Record.Timestamp.Value.UtcDateTime > dateTime) dateTime = tm2Record.Timestamp.Value.UtcDateTime; break;
                case TM2020Record tm2020Record: if (tm2020Record.Timestamp > dateTime) dateTime = tm2020Record.Timestamp; break;
            }
        }

        foreach (var record in changes.RemovedRecords)
        {
            switch (record)
            {
                case TmxReplay tmxReplay: if (tmxReplay.ReplayAt > dateTime) dateTime = tmxReplay.ReplayAt; break;
                case TM2Record tm2Record: if (tm2Record.Timestamp.HasValue && tm2Record.Timestamp.Value.UtcDateTime > dateTime) dateTime = tm2Record.Timestamp.Value.UtcDateTime; break;
                case TM2020Record tm2020Record: if (tm2020Record.Timestamp > dateTime) dateTime = tm2020Record.Timestamp; break;
            }
        }

        return dateTime == DateTime.MinValue ? null : dateTime;
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

        var useLongTimestamp = UseLongTimestamp(changes);

        foreach (var record in newRecords)
        {
            var timestamp = GetTimestamp(record);
            var timestampBracket = timestamp.HasValue ? $" ({timestamp.Value.ToTimestampTag(useLongTimestamp ? Discord.TimestampTagStyles.ShortDateTime : Discord.TimestampTagStyles.ShortTime)})" : "";

            dict.Add(record.Rank.GetValueOrDefault(), $"**{map.GetMdLinkHumanized()}:** ` {record.Rank:00} ` ` {record.Time.ToString(useHundredths: isTMUF)} ` by **{GetDisplayNameMdLink(map, record)}**{timestampBracket}");
        }

        foreach (var (currentRecord, previousRecord) in improvedRecords)
        {
            var delta = (currentRecord.Time - previousRecord.Time).TotalSeconds.ToString(isTMUF ? "0.00" : "0.000");

            var bracket = previousRecord.Rank is null
                ? $"` {delta} `"
                : $"` {delta} `, from ` {previousRecord.Rank:00} `";

            var timestamp = GetTimestamp(currentRecord);
            var timestampBracket = timestamp.HasValue ? $" ({timestamp.Value.ToTimestampTag(useLongTimestamp ? Discord.TimestampTagStyles.ShortDateTime : Discord.TimestampTagStyles.ShortTime)})" : "";

            dict.Add(currentRecord.Rank.GetValueOrDefault(), $"**{map.GetMdLinkHumanized()}:** ` {currentRecord.Rank:00} ` ` {currentRecord.Time.ToString(useHundredths: isTMUF)} ` ({bracket}) by **{GetDisplayNameMdLink(map, currentRecord)}**{timestampBracket}");
        }

        foreach (var record in changes.RemovedRecords)
        {
            dict.Add(record.Rank.GetValueOrDefault(), $"**{map.GetMdLinkHumanized()}:** ` {record.Rank:00} ` ` {record.Time.ToString(useHundredths: isTMUF)} ` by **{GetDisplayNameMdLink(map, record)}** was **removed**");
        }

        foreach (var (_, recStr) in dict)
        {
            yield return recStr;
        }
    }

    private static bool UseLongTimestamp<TPlayerId>(LeaderboardChangesRich<TPlayerId> changes) where TPlayerId : notnull
    {
        var smallestTimestamp = DateTime.MaxValue;
        var biggestTimestamp = DateTime.MinValue;

        foreach (var record in changes.NewRecords)
        {
            var timestamp = GetTimestamp(record);

            if (timestamp.HasValue)
            {
                if (timestamp > biggestTimestamp) biggestTimestamp = timestamp.Value;
                if (timestamp < smallestTimestamp) smallestTimestamp = timestamp.Value;
            }
        }

        foreach (var (currentRecord, previousRecord) in changes.ImprovedRecords)
        {
            var timestamp = GetTimestamp(currentRecord);

            if (timestamp.HasValue)
            {
                if (timestamp > biggestTimestamp) biggestTimestamp = timestamp.Value;
                if (timestamp < smallestTimestamp) smallestTimestamp = timestamp.Value;
            }
        }

        return biggestTimestamp - smallestTimestamp > TimeSpan.FromDays(1);
    }

    private static DateTime? GetTimestamp<TPlayerId>(IRecord<TPlayerId> record) where TPlayerId : notnull => record switch
    {
        TmxReplay tmxReplay => tmxReplay.ReplayAt,
        TM2Record tm2Record => tm2Record.Timestamp?.UtcDateTime,
        TM2020Record tm2020Record => tm2020Record.Timestamp,
        _ => null
    };

    private static string GetDisplayNameMdLink<TPlayerId>(MapModel map, IRecord<TPlayerId> record) where TPlayerId : notnull
    {
        return record is TmxReplay tmxReplay && map.TmxAuthor is not null
            ? tmxReplay.GetDisplayNameMdLink((TmxSite)map.TmxAuthor.Site.Id)
            : record.GetDisplayNameMdLink();
    }

    private async Task ReportToAllScopedDiscordBotsAsync(ReportModel report, IEnumerable<Discord.Embed> embeds, Discord.MessageComponent? components, string scope, CancellationToken cancellationToken)
    {
        var scopePath = scope.Split(':');

        foreach (var reportChannel in await _discordBotUnitOfWork.ReportChannels.GetAllAsync(cancellationToken))
        {
            if (!string.Equals(reportChannel.JoinedGuild.Bot.Guid.ToString(), "e7593b6b-d8f1-4caa-b950-01a8437662d0")
              || string.IsNullOrWhiteSpace(reportChannel.Scope))
            {
                continue;
            }

            var reportScopeSet = ReportScopeSet.FromJson(reportChannel.Scope);

            if (reportScopeSet is null || !ShouldReport(reportScopeSet, scopePath))
            {
                continue;
            }

            try
            {
                await _tmwrDiscordBotService.SendMessageAsync(_discordBotUnitOfWork, reportChannel, snowflake => new ReportChannelMessageModel
                {
                    MessageId = snowflake,
                    ReportGuid = report.Guid,
                    SentOn = DateTime.UtcNow,
                    ModifiedOn = DateTime.UtcNow,
                    Channel = reportChannel
                }, embeds: embeds, components: components, cancellationToken: cancellationToken);
            }
            catch (Discord.Net.HttpException ex)
            {
                _logger.LogWarning(ex, "Failed to send the report with TMWR bot to #{channelName} on {guildName} guild due to Discord API.", reportChannel.Channel.Name, reportChannel.JoinedGuild.Guild.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send the report with TMWR bot to #{channelName} on {guildName} guild.", reportChannel.Channel.Name, reportChannel.JoinedGuild.Guild.Name);
            }
        }

        await _discordBotUnitOfWork.SaveAsync(cancellationToken);
    }

    private async Task ReportToAllScopedDiscordWebhooksAsync(ReportModel report, IEnumerable<Discord.Embed> embeds, string scope, CancellationToken cancellationToken)
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
        if (webhook.Filter is not null)
        {
            return false;
        }

        if (webhook.Scope is null)
        {
            return true;
        }

        return ShouldReport(webhook.Scope, scopePath);
    }

    private static bool ShouldReport(ReportScopeSet scopeSet, string[] reportScopePath)
    {
        var scopeObjLayer = (ReportScope)scopeSet;
        var scopeTypeLayer = scopeSet.GetType();

        // Loops through the scope triggered by the report itself (not the user scope preference)
        // 'scope' is taken as each child in scope sequence
        foreach (var scope in reportScopePath)
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
                    if (scope.StartsWith(param, StringComparison.OrdinalIgnoreCase))
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

            // Temp for further check
            var scopeObjLayerBefore = scopeObjLayer;

            scopeObjLayer = prop.GetValue(scopeObjLayer) as ReportScope;

            // If report scope is simply not present
            if (scopeObjLayer is null)
            {
                // All empty values of ReportScopeSet do not allow reporting at all - it's a special case
                if (scopeTypeLayer == typeof(ReportScopeSet))
                {
                    return false;
                }

                // Possibly check for all properties of scopeTypeLayer for scopeObjLayerBefore
                // If they are all null, then the report is allowed
                return scopeTypeLayer.GetProperties()
                    .Where(x => x.PropertyType.IsSubclassOf(typeof(ReportScope)))
                    .Select(x => x.GetValue(scopeObjLayerBefore) as ReportScope)
                    .All(x => x is null);
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

    public static Discord.EmbedBuilder GetDefaultEmbedBuilder_RemovedWorldRecord(WorldRecordModel? currentWr, IEnumerable<WorldRecordModel> removedWrs)
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

        return builder;
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

        var discordBotMessages = await _discordBotUnitOfWork.ReportChannelMessages
            .GetAllByReportGuidAsync(report.Guid, cancellationToken);

        foreach (var message in discordBotMessages)
        {
            await _tmwrDiscordBotService.ModifyMessageAsync(message, embeds: botEmbed.Yield());
        }
    }

    private static void CreateDiscordEmbeds_NewWorldRecord(WorldRecordModel wr, out Discord.Embed webhookEmbed, out Discord.Embed botEmbed)
    {
        var embedBuilder = GetDefaultEmbedBuilder_NewWorldRecord(wr);

        webhookEmbed = embedBuilder.Build();
        botEmbed = embedBuilder.WithFooter(wr.Guid.ToString(), UrlConsts.Favicon).Build();
    }

    private static void CreateDiscordEmbeds_RemovedWorldRecord(WorldRecordModel? currentWr, IEnumerable<WorldRecordModel> removedWrs, out Discord.Embed webhookEmbed, out Discord.Embed botEmbed)
    {
        var embedBuilder = GetDefaultEmbedBuilder_RemovedWorldRecord(currentWr, removedWrs);

        webhookEmbed = embedBuilder.Build();

        if (currentWr is not null)
        {
            embedBuilder.WithFooter(currentWr.Guid.ToString(), UrlConsts.Favicon);
        }
        else
        {
            embedBuilder.WithFooter("");
        }

        botEmbed = embedBuilder.Build();
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

    public async Task RemoveWorldRecordReportAsync(WorldRecordModel wr, CancellationToken cancellationToken = default)
    {
        var report = await _wrUnitOfWork.Reports.GetByWorldRecordAsync(wr, cancellationToken);

        if (report is null)
        {
            return;
        }

        var discordWebhookMessages = await _wrUnitOfWork.DiscordWebhookMessages
            .GetAllByReportAsync(report, cancellationToken);

        foreach (var message in discordWebhookMessages)
        {
            await _discordWebhookService.DeleteMessageAsync(message, cancellationToken);
        }

        var discordBotMessages = await _discordBotUnitOfWork.ReportChannelMessages
            .GetAllByReportGuidAsync(report.Guid, cancellationToken);

        foreach (var message in discordBotMessages)
        {
            await _tmwrDiscordBotService.DeleteMessageAsync(message);
        }

        await _discordBotUnitOfWork.SaveAsync(cancellationToken);
    }
}
