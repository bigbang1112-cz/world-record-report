using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using BigBang1112.WorldRecordReportLib.Enums;
using BigBang1112.WorldRecordReportLib.Models;
using BigBang1112.WorldRecordReportLib.Services;
using Discord;
using TmEssentials;
using Discord.WebSocket;
using BigBang1112.DiscordBot.Models;
using BigBang1112.WorldRecordReportLib.TMWR.Models;

using Game = BigBang1112.WorldRecordReportLib.Enums.Game;

namespace BigBang1112.WorldRecordReportLib.TMWR.Commands;

[DiscordBotCommand("top10", "Shows the Top 10 world leaderboard.")]
public class Top10Command : MapRelatedWithUidCommand
{
    private readonly TmwrDiscordBotService _tmwrDiscordBotService;
    private readonly IWrUnitOfWork _wrUnitOfWork;
    private readonly RecordStorageService _recordStorageService;

    public Top10Command(TmwrDiscordBotService tmwrDiscordBotService,
                        IWrUnitOfWork wrUnitOfWork,
                        RecordStorageService recordStorageService) : base(tmwrDiscordBotService, wrUnitOfWork)
    {
        _tmwrDiscordBotService = tmwrDiscordBotService;
        _wrUnitOfWork = wrUnitOfWork;
        _recordStorageService = recordStorageService;
    }
    
    [DiscordBotCommandOption("showtimestamps", ApplicationCommandOptionType.Boolean, "Show timestamps of when each record was driven.")]
    public bool ShowTimestamps { get; set; }

    protected override async Task BuildEmbedResponseAsync(MapModel map, EmbedBuilder builder)
    {
        builder.Footer.Text = $"{builder.Footer.Text}{(ShowTimestamps ? $" (ShowTimestamps)" : "")}";
        builder.Title = map.GetHumanizedDeformattedName();
        builder.Url = map.GetInfoUrl();

        if (!ShowTimestamps)
        {
            builder.ThumbnailUrl = map.GetThumbnailUrl();
        }

        if (!await CreateTop10EmbedContentAsync(map, builder))
        {
            builder.Description = "No leaderboard found.";
        }
    }

    protected override async Task<ComponentBuilder?> CreateComponentsAsync(MapModel map, bool isModified)
    {
        var miniRecords = Enumerable.Empty<MiniRecord>();
        
        if (map.Game.IsTM2020())
        {
            var tm2020recs = await _recordStorageService.GetTM2020LeaderboardAsync(map.MapUid);

            if (tm2020recs is not null)
            {
                miniRecords = GetMiniRecordsFromTM2020Leaderboard(tm2020recs);
            }
        }

        if (map.Game.IsTM2())
        {
            var recordSet = await _recordStorageService.GetTM2LeaderboardAsync(map.MapUid);

            if (recordSet is not null)
            {
                var logins = await FetchLoginModelsAsync(recordSet);
                miniRecords = GetMiniRecordsFromRecordSet(recordSet.Records, logins);
            }
        }

        var isTMUF = map.Game.IsTMUF();

        if (isTMUF)
        {
            if (map.TmxAuthor is null)
            {
                return new ComponentBuilder();
            }

            var recordSetTmx = await _recordStorageService.GetTmxLeaderboardAsync((TmxSite)map.TmxAuthor.Site.Id, map.MapUid);

            if (recordSetTmx is null)
            {
                return new ComponentBuilder();
            }

            var top10records = GetTop10(recordSetTmx);
            miniRecords = GetMiniRecordsFromTmxReplays(top10records, map, formattable: false);
        }

        if (!miniRecords.Any())
        {
            return null;
        }

        var isStunts = map.IsStuntsMode();

        var selectMenuBuilder = new SelectMenuBuilder
        {
            CustomId = CreateCustomId("rec"),
            Placeholder = "Select a record...",

            Options = miniRecords.Select((x, i) =>
            {
                return new SelectMenuOptionBuilder(isStunts ? x.TimeOrScore.ToString() : new TimeInt32(x.TimeOrScore).ToString(useHundredths: isTMUF),
                    $"{map.MapUid}-{i}", $"by {x.Nickname}");
            }).ToList()
        };

        return new ComponentBuilder().WithSelectMenu(selectMenuBuilder);
    }

    private static IEnumerable<MiniRecord> GetMiniRecordsFromTM2020Leaderboard(ReadOnlyCollection<TM2020Record> records)
    {
        foreach (var record in records.Where(x => !x.Ignored).Take(10))
        {
            yield return new MiniRecord(record.Rank, record.Time.TotalMilliseconds, record.DisplayName ?? record.PlayerId.ToString());
        }
    }

    private async Task<bool> CreateTop10EmbedContentAsync(MapModel map, EmbedBuilder builder)
    {
        switch ((Game)map.Game.Id)
        {
            case Game.TM2:
                if (!await CreateTop10EmbedContentFromTM2Async(map, builder))
                {
                    return false;
                }
                break;
            case Game.TMUF:
                if (!await CreateTop10EmbedContentFromTmxAsync(map, builder))
                {
                    return false;
                }
                break;
            case Game.TM2020:
                var leaderboard = await _recordStorageService.GetTM2020LeaderboardAsync(map.MapUid);

                if (leaderboard is null)
                {
                    return false;
                }

                CreateTop10EmbedContentFromTM2020(leaderboard, builder);
                break;
        }

        if (map.LastRefreshedOn is not null)
        {
            builder.AddField("Last refreshed on", map.LastRefreshedOn.Default.ToTimestampTag(), inline: true);
        }

        var lastUpdatedOn = _recordStorageService.GetOfficialLeaderboardLastUpdatedOn((Game)map.Game.Id, map.MapUid);

        if (lastUpdatedOn.HasValue)
        {
            builder.AddField("Last updated on", lastUpdatedOn.Value.ToTimestampTag(), inline: true);
        }

        return true;
    }

    private void CreateTop10EmbedContentFromTM2020(IEnumerable<TM2020Record> leaderboard, EmbedBuilder builder)
    {
        var top10records = leaderboard.Where(x => !x.Ignored).Take(10)
            .Select((x, i) => new MiniRecord(Rank: i + 1, x.Time.TotalMilliseconds, Nickname: $"[{x.DisplayName ?? x.PlayerId.ToString()}](https://trackmania.io/#/player/{x.PlayerId})", x.Timestamp));
        
        var miniRecordStrings = ConvertMiniRecordsToStrings(top10records, isTMUF: false, isStunts: false);

        builder.Description = string.Join('\n', miniRecordStrings);
    }

    private async Task<bool> CreateTop10EmbedContentFromTmxAsync(MapModel map, EmbedBuilder builder)
    {
        if (map.TmxAuthor is null)
        {
            return false;
        }

        var recordSetTmx = await _recordStorageService.GetTmxLeaderboardAsync((TmxSite)map.TmxAuthor.Site.Id, map.MapUid);

        if (recordSetTmx is null)
        {
            return false;
        }

        var top10records = GetTop10(recordSetTmx);
        var miniRecords = GetMiniRecordsFromTmxReplays(top10records, map, formattable: true);
        var miniRecordStrings = ConvertMiniRecordsToStrings(miniRecords, map.Game.IsTMUF(), map.IsStuntsMode());

        builder.Description = string.Join('\n', miniRecordStrings);

        return true;
    }

    private static IEnumerable<TmxReplay> GetTop10(IEnumerable<TmxReplay> recordSetTmx)
    {
        return recordSetTmx.Where(x => x.Rank is not null).Take(10);
    }

    private async Task<bool> CreateTop10EmbedContentFromTM2Async(MapModel map, EmbedBuilder builder)
    {
        var recordSet = await _recordStorageService.GetTM2LeaderboardAsync(map.MapUid);
        
        if (recordSet is null)
        {
            return false;
        }

        var loginDictionary = await FetchLoginModelsAsync(recordSet);
        var miniRecords = GetMiniRecordsFromRecordSet(recordSet.Records, loginDictionary);
        var miniRecordStrings = ConvertMiniRecordsToStrings(miniRecords, map.Game.IsTMUF(), map.IsStuntsMode());

        builder.Description = string.Join('\n', miniRecordStrings);

        return true;
    }

    private static IEnumerable<MiniRecord> GetMiniRecordsFromTmxReplays(IEnumerable<TmxReplay> records, MapModel map, bool formattable)
    {
        var tmxUrl = map.TmxAuthor?.Site.Url;

        foreach (var record in records)
        {
            var userId = record.UserId;
            var displayName = record.UserName ?? userId.ToString();

            var displayNameFormatted = !formattable || tmxUrl is null
                ? displayName
                : $"[{displayName.EscapeDiscord()}]({tmxUrl}usershow/{userId})";

            yield return new MiniRecord(record.Rank.GetValueOrDefault(), map.IsStuntsMode() ? record.ReplayScore : record.ReplayTime.TotalMilliseconds, displayNameFormatted, record.ReplayAt);
        }
    }

    private static IEnumerable<MiniRecord> GetMiniRecordsFromRecordSet(IEnumerable<TM2Record> records, IDictionary<string, LoginModel> loginDictionary)
    {
        foreach (var record in records)
        {
            var displayName = record.Login;

            if (loginDictionary.TryGetValue(displayName, out LoginModel? loginModel))
            {
                displayName = loginModel.GetDeformattedNickname().EscapeDiscord();
            }

            yield return new MiniRecord(record.Rank, record.Time.TotalMilliseconds, displayName);
        }
    }

    private IEnumerable<string> ConvertMiniRecordsToStrings(IEnumerable<MiniRecord> records, bool isTMUF, bool isStunts)
    {
        foreach (var record in records)
        {
            yield return $"` {record.Rank:00} ` **` {(isStunts ? record.TimeOrScore : new TimeInt32(record.TimeOrScore).ToString(useHundredths: isTMUF))} `** by **{record.Nickname}**{(ShowTimestamps && record.Timestamp.HasValue ? $" ({record.Timestamp.Value.ToTimestampTag(TimestampTagStyles.ShortDate)})" : "")}";
        }
    }

    private async Task<Dictionary<string, LoginModel>> FetchLoginModelsAsync(LeaderboardTM2 recordSet)
    {
        return await _wrUnitOfWork.Logins.GetByNamesAsync(Game.TM2, recordSet.Records.Select(x => x.Login));
    }

    public override async Task<DiscordBotMessage?> SelectMenuAsync(SocketMessageComponent messageComponent, Deferer deferer)
    {
        var customIdMap = CreateCustomId("map");
        
        if (messageComponent.Data.CustomId == customIdMap)
        {
            var footerText = messageComponent.Message.Embeds.FirstOrDefault()?.Footer?.Text;

            if (footerText is null)
            {
                return null;
            }

            var match = Regex.Match(footerText, @"(?<MapUid>.*)(?:\s)(?:\((?<ShowTimestamps>ShowTimestamps)\))?");

            if (match.Groups.TryGetValue("ShowTimestamps", out Group? showTimestampsGroup))
            {
                ShowTimestamps = showTimestampsGroup.Success;
            }

            // maybe validate MapUid

            return await base.SelectMenuAsync(messageComponent, deferer);
        }

        var customIdRec = CreateCustomId("rec");

        if (messageComponent.Data.CustomId != customIdRec)
        {
            return await base.SelectMenuAsync(messageComponent, deferer);
        }

        var split = messageComponent.Data.Values.First().Split('-');

        if (split.Length < 2)
        {
            return new DiscordBotMessage(new EmbedBuilder().WithDescription("Not enough data for the command.").Build(),
                ephemeral: true, alwaysPostAsNewMessage: true);
        }

        var mapUid = split[0];
        var rank = long.Parse(split[1]) + 1;

        using var scope = _tmwrDiscordBotService.CreateCommand(out RecordCommand? recordCommand);

        if (recordCommand is null)
        {
            throw new Exception();
        }

        recordCommand.MapUid = mapUid;
        recordCommand.Rank = rank;

        var message = await recordCommand.ExecuteAsync(messageComponent, deferer);

        return message with { AlwaysPostAsNewMessage = true, Ephemeral = true };
    }
}
