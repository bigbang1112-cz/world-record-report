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
    private readonly IWrRepo _repo;
    private readonly IRecordSetService _recordSetService;

    public Top10Command(TmwrDiscordBotService tmwrDiscordBotService, IWrRepo repo, IRecordSetService recordSetService) : base(tmwrDiscordBotService, repo)
    {
        _repo = repo;
        _recordSetService = recordSetService;
    }

    protected override async Task BuildEmbedResponseAsync(MapModel map, EmbedBuilder builder)
    {
        var recordSet = await _recordSetService.GetFromMapAsync("World", map.MapUid);

        var desc = recordSet is null
            ? "No leaderboard found."
            : await CreateTop10DescAsync(recordSet, map.Game.IsTMUF());

        builder.Title = map.GetHumanizedDeformattedName();
        builder.Description = desc;

        var thumbnailUrl = map.GetThumbnailUrl();

        if (thumbnailUrl is not null)
        {
            builder.ThumbnailUrl = thumbnailUrl;
        }

        if (recordSet is not null)
        {
            builder.AddField("Record count", recordSet.GetRecordCount().ToString("N0"));
        }
    }

    protected override async Task<ComponentBuilder?> CreateComponentsAsync(MapModel map, bool isModified)
    {
        var recordSet = await _recordSetService.GetFromMapAsync("World", map.MapUid);
        
        if (recordSet is null)
        {
            if (isModified)
            {
                return new ComponentBuilder();
            }

            return null;
        }

        var isTMUF = map.Game.IsTMUF();

        var logins = await FetchLoginModelsFromRecordSetAsync(recordSet);

        var selectMenuBuilder = new SelectMenuBuilder()
            .WithCustomId(CreateCustomId("rec"))
            .WithPlaceholder("Select a record...")
            .WithOptions(recordSet.Records.Select((x, i) =>
            {
                var nickname = x.Login;

                if (logins.TryGetValue(x.Login, out LoginModel? loginModel))
                {
                    nickname = loginModel.GetDeformattedNickname();
                }

                return new SelectMenuOptionBuilder(new TimeInt32(x.Time).ToString(useHundredths: isTMUF), $"{map.MapUid}-{i}", $"by {nickname}");
            }).ToList());

        return new ComponentBuilder().WithSelectMenu(selectMenuBuilder);
    }

    private async Task<IEnumerable<string>> CreateTop10EnumerableAsync(RecordSet recordSet, bool isTMUF)
    {
        var loginDictionary = await FetchLoginModelsFromRecordSetAsync(recordSet);

        return recordSet.Records.Select(x =>
        {
            var login = x.Login;

            if (loginDictionary.TryGetValue(login, out LoginModel? loginModel))
            {
                login = loginModel.GetDeformattedNickname();
            }

            return $"{x.Rank}. **{new TimeInt32(x.Time).ToString(useHundredths: isTMUF)}** by {login}";
        });
    }

    private async Task<Dictionary<string, LoginModel>> FetchLoginModelsFromRecordSetAsync(RecordSet recordSet)
    {
        var loginDictionary = new Dictionary<string, LoginModel>();

        foreach (var login in recordSet.Records.Select(x => x.Login))
        {
            var loginModel = await _repo.GetLoginAsync(login);

            if (loginModel is null)
            {
                continue;
            }

            loginDictionary[login] = loginModel;
        }

        return loginDictionary;
    }

    private async Task<string> CreateTop10DescAsync(RecordSet recordSet, bool isTMUF)
    {
        return string.Join('\n', await CreateTop10EnumerableAsync(recordSet, isTMUF));
    }

    public override async Task<DiscordBotMessage?> SelectMenuAsync(SocketMessageComponent messageComponent)
    {
        var customIdRec = CreateCustomId("rec");

        if (messageComponent.Data.CustomId == customIdRec)
        {
            var embed = new EmbedBuilder()
                .WithTitle(messageComponent.Data.Values.First())
                .Build();

            return new DiscordBotMessage(embed, ephemeral: true, alwaysPostAsNewMessage: true);
        }

        return await base.SelectMenuAsync(messageComponent);
    }
}
