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

    public Top10Command(TmwrDiscordBotService tmwrDiscordBotService,
                        IWrRepo repo,
                        IRecordSetService recordSetService,
                        ITmxRecordSetService tmxRecordSetService) : base(tmwrDiscordBotService, repo)
    {
        _tmwrDiscordBotService = tmwrDiscordBotService;
        _repo = repo;
        _recordSetService = recordSetService;
        _tmxRecordSetService = tmxRecordSetService;
    }

    protected override async Task BuildEmbedResponseAsync(MapModel map, EmbedBuilder builder)
    {
        builder.Title = map.GetHumanizedDeformattedName();
        builder.Url = map.GetTmxUrl();
        builder.ThumbnailUrl = map.GetThumbnailUrl();

        var embedContent = await CreateTop10EmbedContentAsync(map);

        if (embedContent.HasValue)
        {
            builder.Description = embedContent.Value.desc;

            if (embedContent.Value.recordCount.HasValue)
            {
                builder.AddField("Record count", embedContent.Value.recordCount.Value.ToString("N0"));
            }
        }
        else
        {
            builder.Description = "No leaderboard found.";
        }
    }

    protected override async Task<ComponentBuilder?> CreateComponentsAsync(MapModel map, bool isModified)
    {
        var recordSet = await _recordSetService.GetFromMapAsync("World", map.MapUid);

        IEnumerable<MiniRecord> miniRecords;

        if (recordSet is null)
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
            var logins = await FetchTmxLoginModelsAsync(top10records);
            miniRecords = GetMiniRecordsFromTmxReplays(top10records, logins, map.IsStuntsMode());
        }
        else
        {
            var logins = await FetchLoginModelsAsync(recordSet);
            miniRecords = GetMiniRecordsFromRecordSet(recordSet.Records, logins);
        }

        var isTMUF = map.Game.IsTMUF();
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

    private async Task<(string desc, int? recordCount)?> CreateTop10EmbedContentAsync(MapModel map)
    {
        var recordSet = await _recordSetService.GetFromMapAsync("World", map.MapUid);

        if (recordSet is not null)
        {
            return await CreateTop10EmbedContentFromTM2Async(map, recordSet);
        }

        if (map.TmxAuthor is null)
        {
            return null;
        }

        var recordSetTmx = await _tmxRecordSetService.GetRecordSetAsync(map.TmxAuthor.Site, map);

        if (recordSetTmx is null)
        {
            return null;
        }

        return await CreateTop10EmbedContentFromTmxAsync(map, recordSetTmx);
    }

    private async Task<(string desc, int? recordCount)?> CreateTop10EmbedContentFromTmxAsync(MapModel map, TmxReplay[] recordSetTmx)
    {
        var top10records = GetTop10(recordSetTmx);
        var tmxLoginDictionary = await FetchTmxLoginModelsAsync(top10records);
        var miniRecords = GetMiniRecordsFromTmxReplays(top10records, tmxLoginDictionary, map.IsStuntsMode());
        var miniRecordStrings = ConvertMiniRecordsToStrings(miniRecords, map.Game.IsTMUF(), map.IsStuntsMode());

        var desc = string.Join('\n', miniRecordStrings);

        return (desc, null);
    }

    private static IEnumerable<TmxReplay> GetTop10(IEnumerable<TmxReplay> recordSetTmx)
    {
        return recordSetTmx.Where(x => x.Rank is not null).Take(10);
    }

    private async Task<(string desc, int? recordCount)?> CreateTop10EmbedContentFromTM2Async(MapModel map, RecordSet recordSet)
    {
        var loginDictionary = await FetchLoginModelsAsync(recordSet);
        var miniRecords = GetMiniRecordsFromRecordSet(recordSet.Records, loginDictionary);
        var miniRecordStrings = ConvertMiniRecordsToStrings(miniRecords, map.Game.IsTMUF(), map.IsStuntsMode());

        var desc = string.Join('\n', miniRecordStrings);

        return (desc, recordSet.GetRecordCount());
    }

    private static IEnumerable<MiniRecord> GetMiniRecordsFromTmxReplays(IEnumerable<TmxReplay> records, Dictionary<int, TmxLoginModel> tmxLoginDictionary, bool isStunts)
    {
        foreach (var record in records)
        {
            var userId = record.UserId;

            var displayName = tmxLoginDictionary.TryGetValue(userId, out TmxLoginModel? loginModel)
                ? loginModel.Nickname?.EscapeDiscord() ?? loginModel.UserId.ToString()
                : userId.ToString();

            yield return new MiniRecord(record.Rank.GetValueOrDefault(), isStunts ? record.ReplayScore : record.ReplayTime, displayName);
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
            yield return $"{record.Rank}) **{(isStunts ? record.TimeOrScore : new TimeInt32(record.TimeOrScore).ToString(useHundredths: isTMUF))}** by {record.Nickname}";
        }
    }

    private async Task<Dictionary<int, TmxLoginModel>> FetchTmxLoginModelsAsync(IEnumerable<TmxReplay> recordSetTmx)
    {
        var loginDictionary = new Dictionary<int, TmxLoginModel>();

        foreach (var userId in recordSetTmx.Select(x => x.UserId))
        {
            var loginModel = await _repo.GetTmxLoginAsync(userId);

            if (loginModel is not null)
            {
                loginDictionary[userId] = loginModel;
            }
        }

        return loginDictionary;
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
