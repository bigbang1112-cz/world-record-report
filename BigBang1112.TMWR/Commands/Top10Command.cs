using System.Collections.ObjectModel;
using BigBang1112.Extensions;
using BigBang1112.TMWR.Models;
using BigBang1112.WorldRecordReportLib.Models;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;
using BigBang1112.WorldRecordReportLib.Services;
using Discord;
using TmEssentials;

namespace BigBang1112.TMWR.Commands;

[DiscordBotCommand("top10", "Shows the Top 10 world leaderboard.")]
public class Top10Command : MapRelatedWithUidCommand
{
    private readonly TmwrDiscordBotService _tmwrDiscordBotService;
    private readonly IWrRepo _repo;
    private readonly IRecordSetService _recordSetService;
    private readonly ITmxRecordSetService _tmxRecordSetService;
    private readonly RecordStorageService _recordStorageService;

    public Top10Command(TmwrDiscordBotService tmwrDiscordBotService,
                        IWrRepo repo,
                        IRecordSetService recordSetService,
                        ITmxRecordSetService tmxRecordSetService,
                        RecordStorageService recordStorageService) : base(tmwrDiscordBotService, repo)
    {
        _tmwrDiscordBotService = tmwrDiscordBotService;
        _repo = repo;
        _recordSetService = recordSetService;
        _tmxRecordSetService = tmxRecordSetService;
        _recordStorageService = recordStorageService;
    }

    protected override async Task BuildEmbedResponseAsync(MapModel map, EmbedBuilder builder)
    {
        builder.Title = map.GetHumanizedDeformattedName();
        builder.Url = map.GetInfoUrl();
        builder.ThumbnailUrl = map.GetThumbnailUrl();

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
            var recordSet = await _recordSetService.GetFromMapAsync("World", map.MapUid);

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

            var recordSetTmx = await _tmxRecordSetService.GetRecordSetAsync(map.TmxAuthor.Site, map);

            if (recordSetTmx is null)
            {
                return new ComponentBuilder();
            }

            var top10records = GetTop10(recordSetTmx);
            miniRecords = GetMiniRecordsFromTmxReplays(top10records, map, formattable: false);
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
        if (map.Game.IsTM2020())
        {
            var leaderboard = await _recordStorageService.GetTM2020LeaderboardAsync(map.MapUid);
            
            if (leaderboard is null)
            {
                return false;
            }

            CreateTop10EmbedContentFromTM2020(leaderboard, builder);

            if (map.LastRefreshedOn is not null)
            {
                builder.AddField("Last refreshed on", map.LastRefreshedOn.Default.ToTimestampTag(), inline: true);
            }

            var lastUpdatedOn = _recordStorageService.GetTM2020LeaderboardLastUpdatedOn(map.MapUid);

            if (lastUpdatedOn.HasValue)
            {
                builder.AddField("Last updated on", lastUpdatedOn.Value.ToTimestampTag(), inline: true);
            }

            return true;
        }

        var recordSet = await _recordSetService.GetFromMapAsync("World", map.MapUid);

        if (recordSet is not null)
        {
            await CreateTop10EmbedContentFromTM2Async(map, recordSet, builder);

            return true;
        }

        if (map.TmxAuthor is null)
        {
            return false;
        }

        var recordSetTmx = await _tmxRecordSetService.GetRecordSetAsync(map.TmxAuthor.Site, map);

        if (recordSetTmx is null)
        {
            return false;
        }

        CreateTop10EmbedContentFromTmx(map, recordSetTmx, builder);

        return true;
    }

    private static void CreateTop10EmbedContentFromTM2020(IEnumerable<TM2020Record> leaderboard, EmbedBuilder builder)
    {
        var top10records = leaderboard.Where(x => !x.Ignored).Take(10)
            .Select((x, i) => new MiniRecord(Rank: i + 1, x.Time.TotalMilliseconds, Nickname: $"[{x.DisplayName ?? x.PlayerId.ToString()}](https://trackmania.io/#/player/{x.PlayerId})"));
        
        var miniRecordStrings = ConvertMiniRecordsToStrings(top10records, isTMUF: false, isStunts: false);

        builder.Description = string.Join('\n', miniRecordStrings);
    }

    private static void CreateTop10EmbedContentFromTmx(MapModel map, TmxReplay[] recordSetTmx, EmbedBuilder builder)
    {
        var top10records = GetTop10(recordSetTmx);
        var miniRecords = GetMiniRecordsFromTmxReplays(top10records, map, formattable: true);
        var miniRecordStrings = ConvertMiniRecordsToStrings(miniRecords, map.Game.IsTMUF(), map.IsStuntsMode());

        builder.Description = string.Join('\n', miniRecordStrings);
    }

    private static IEnumerable<TmxReplay> GetTop10(IEnumerable<TmxReplay> recordSetTmx)
    {
        return recordSetTmx.Where(x => x.Rank is not null).Take(10);
    }

    private async Task CreateTop10EmbedContentFromTM2Async(MapModel map, RecordSet recordSet, EmbedBuilder builder)
    {
        var loginDictionary = await FetchLoginModelsAsync(recordSet);
        var miniRecords = GetMiniRecordsFromRecordSet(recordSet.Records, loginDictionary);
        var miniRecordStrings = ConvertMiniRecordsToStrings(miniRecords, map.Game.IsTMUF(), map.IsStuntsMode());

        builder.Description = string.Join('\n', miniRecordStrings);
        builder.AddField("Record count", recordSet.GetRecordCount());
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

            yield return new MiniRecord(record.Rank.GetValueOrDefault(), map.IsStuntsMode() ? record.ReplayScore : record.ReplayTime, displayNameFormatted);
        }
    }

    private static IEnumerable<MiniRecord> GetMiniRecordsFromRecordSet(IEnumerable<RecordSetDetailedRecord> records, IDictionary<string, LoginModel> loginDictionary)
    {
        foreach (var record in records)
        {
            var displayName = record.Login;

            if (loginDictionary.TryGetValue(displayName, out LoginModel? loginModel))
            {
                displayName = loginModel.GetDeformattedNickname().EscapeDiscord();
            }

            yield return new MiniRecord(record.Rank, record.Time, displayName);
        }
    }

    private static IEnumerable<string> ConvertMiniRecordsToStrings(IEnumerable<MiniRecord> records, bool isTMUF, bool isStunts)
    {
        foreach (var record in records)
        {
            yield return $"` {record.Rank:00} ` **` {(isStunts ? record.TimeOrScore : new TimeInt32(record.TimeOrScore).ToString(useHundredths: isTMUF))} `** by **{record.Nickname}**";
        }
    }

    private async Task<Dictionary<string, LoginModel>> FetchLoginModelsAsync(RecordSet recordSet)
    {
        var loginDictionary = new Dictionary<string, LoginModel>();

        foreach (var login in recordSet.Records.Select(x => x.Login))
        {
            var loginModel = await _repo.GetLoginAsync(login);

            if (loginModel is not null)
            {
                loginDictionary[login] = loginModel;
            }
        }

        return loginDictionary;
    }

    public override async Task<DiscordBotMessage?> SelectMenuAsync(SocketMessageComponent messageComponent, Deferer deferer)
    {
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
