using BigBang1112.Attributes.DiscordBot;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;
using BigBang1112.WorldRecordReportLib.Services;
using Discord;
using Discord.WebSocket;
using TmEssentials;

namespace BigBang1112.TMWR.Commands;

[DiscordBotCommand("top10", "Shows the Top 10 world leaderboard.")]
public class Top10Command : MapRelatedCommand
{
    private readonly IWrRepo _repo;
    private readonly IRecordSetService _recordSetService;

    public Top10Command(IWrRepo repo, IRecordSetService recordSetService) : base(repo)
    {
        _repo = repo;
        _recordSetService = recordSetService;
    }

    public override async Task SelectMenuAsync(SocketMessageComponent messageComponent, IReadOnlyCollection<string> values)
    {
        var mapUid = values.First();

        var map = await _repo.GetMapByUidAsync(mapUid);

        if (map is null)
        {
            return;
        }

        var mapNames = await _repo.GetMapNamesAsync(map.DeformattedName);

        var embed = await CreateEmbedAsync(map, mapNames.Count > 1);

        await messageComponent.UpdateAsync(x =>
        {
            x.Embed = embed;
        });
    }

    public async Task<Embed> CreateEmbedAsync(MapModel map, bool hasMultipleSameNames)
    {
        var recordSet = await _recordSetService.GetFromMapAsync("World", map.MapUid);

        var desc = "";

        if (recordSet is null)
        {
            desc = "No record set found.";
        }
        else
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

            desc = string.Join('\n', recordSet.Records.Select(x =>
            {
                var login = x.Login;

                if (loginDictionary.TryGetValue(login, out LoginModel? loginModel))
                {
                    login = loginModel.GetDeformattedNickname();
                }

                return $"{x.Rank}. **{new TimeInt32(x.Time)}** by {login}";
            }));
        }

        return new EmbedBuilder()
            .WithTitle(hasMultipleSameNames ? $"{map.DeformattedName}  [{map.GetTitleUidOrEnvironment()}]" : map.DeformattedName)
            .WithDescription(desc)
            .Build();
    }

    public override async Task ExecuteWithMapsAsync(SocketSlashCommand slashCommand, List<MapModel> mapsForMenu)
    {
        var map = mapsForMenu.First();

        var lookup = mapsForMenu.ToLookup(x => x.DeformattedName);

        var hasMultipleSameNames = lookup[map.DeformattedName].Count() > 1;

        var embed = await CreateEmbedAsync(map, hasMultipleSameNames);

        var component = default(MessageComponent);

        if (mapsForMenu.Count > 1)
        {
            var menuBuilder = new SelectMenuBuilder()
                .WithPlaceholder("Select other map...")
                .WithCustomId(slashCommand.CommandName);

            foreach (var m in mapsForMenu)
            {
                var label = hasMultipleSameNames
                    ? $"{m.DeformattedName} [{m.GetTitleUidOrEnvironment()}]"
                    : m.DeformattedName;

                menuBuilder.AddOption(label, m.MapUid, description: $"by {m.Author.GetDeformattedNickname()}");
            }

            component = new ComponentBuilder()
                .WithSelectMenu(menuBuilder)
                .Build();
        }

        await slashCommand.RespondAsync("Here is the result:", embed: embed, components: component);
    }
}
