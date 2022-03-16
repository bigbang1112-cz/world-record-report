using BigBang1112.Extensions;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;
using Discord;

namespace BigBang1112.TMWR.Commands;

public abstract class MapRelatedCommand : DiscordBotCommand
{
    private readonly IWrRepo _repo;

    protected MapRelatedCommand(TmwrDiscordBotService tmwrDiscordBotService, IWrRepo repo) : base(tmwrDiscordBotService)
    {
        _repo = repo;
    }

    [DiscordBotCommandOption("name", ApplicationCommandOptionType.String, "Name of the map.")]
    public string? MapName { get; set; }

    public async Task<IEnumerable<string>> AutocompleteMapNameAsync(string value)
    {
        return await _repo.GetMapNamesAsync(value);
    }

    [DiscordBotCommandOption("env", ApplicationCommandOptionType.String, "Environment of the map.")]
    public string? Environment { get; set; }

    public async Task<IEnumerable<string>> AutocompleteEnvironmentAsync(string value)
    {
        return await _repo.GetEnvNamesAsync(value);
    }

    [DiscordBotCommandOption("title", ApplicationCommandOptionType.String, "Title pack of the map.")]
    public string? TitlePack { get; set; }

    public async Task<IEnumerable<string>> AutocompleteTitlePackAsync(string value)
    {
        return await _repo.GetTitlePacksAsync(value);
    }

    public override async Task<DiscordBotMessage> ExecuteAsync(SocketSlashCommand slashCommand)
    {
        var maps = await _repo.GetMapsByMultipleParamsAsync(MapName, Environment, TitlePack);

        if (maps.Any())
        {
            return await CreateResponseMessageWithMapsParamAsync(maps);
        }

        return CreateResponseMessageNoMapsFound();
    }

    protected static DiscordBotMessage CreateResponseMessageNoMapsFound()
    {
        var embed = new EmbedBuilder()
            .WithTitle("No maps were found")
            .WithDescription("Try different set of filters.")
            .Build();

        return new DiscordBotMessage(embed, ephemeral: true);
    }

    protected async Task<DiscordBotMessage> CreateResponseMessageWithMapsParamAsync(IEnumerable<MapModel> maps)
    {
        var map = maps.First();

        var embed = await CreateEmbedResponseAsync(map);

        var builder = await CreateComponentsAsync(map, isModified: false);

        if (maps.Count() > 1)
        {
            var lookup = maps.ToLookup(x => x.DeformattedName);

            var customId = CreateCustomId("map");

            builder ??= new ComponentBuilder();
            builder.WithSelectMenu(CreateSelectMenu(customId, maps, lookup));
        }

        return new DiscordBotMessage(embed, builder?.Build());
    }

    protected virtual Task<ComponentBuilder?> CreateComponentsAsync(MapModel map, bool isModified)
    {
        return Task.FromResult<ComponentBuilder?>(null);
    }

    protected virtual async Task<Embed> CreateEmbedResponseAsync(MapModel map)
    {
        var builder = new EmbedBuilder();
        builder.WithBotFooter(map.MapUid);

        await BuildEmbedResponseAsync(map, builder);

        return builder.Build();
    }

    protected virtual Task BuildEmbedResponseAsync(MapModel map, EmbedBuilder builder)
    {
        builder.Title = "Map-related command not implemented.";

        return Task.CompletedTask;
    }

    private static SelectMenuBuilder CreateSelectMenu(string customId, IEnumerable<MapModel> mapsForMenu, ILookup<string, MapModel> lookup)
    {
        var menuBuilder = new SelectMenuBuilder()
            .WithPlaceholder("Select other map...")
            .WithCustomId(customId);

        foreach (var m in mapsForMenu)
        {
            var deformattedName = m.GetHumanizedDeformattedName();

            var hasMultipleSameNames = lookup[deformattedName].Count() > 1;

            var label = hasMultipleSameNames
                ? $"{deformattedName} [{m.GetTitleUidOrEnvironment()}]"
                : deformattedName;

            menuBuilder.AddOption(label, m.MapUid, description: $"by {m.Author.GetDeformattedNickname()}");
        }

        return menuBuilder;
    }

    public override async Task<DiscordBotMessage?> SelectMenuAsync(SocketMessageComponent messageComponent)
    {
        var customIdMap = CreateCustomId("map");

        if (messageComponent.Data.CustomId == customIdMap)
        {
            return await SelectMenuMapAsync(messageComponent, customIdMap);
        }

        return null;
    }

    private async Task<DiscordBotMessage> SelectMenuMapAsync(SocketMessageComponent messageComponent, string customIdMap)
    {
        var mapUid = messageComponent.Data.Values.First();

        var map = await _repo.GetMapByUidAsync(mapUid);

        if (map is null)
        {
            return new DiscordBotMessage(new EmbedBuilder()
                .WithTitle("Map not found.")
                .WithDescription("The map has been likely removed from the database.")
                .Build(), ephemeral: true, alwaysPostAsNewMessage: true);
        }

        var componentBuilder = await CreateComponentsAsync(map, isModified: true);

        if (componentBuilder is not null)
        {
            var mapSelectMenu = messageComponent.Message
                .Components
                .SelectMany(x => x.Components)
                .FirstOrDefault(x => x.CustomId == customIdMap)
                as SelectMenuComponent;

            if (mapSelectMenu is not null)
            {
                componentBuilder.WithSelectMenu(mapSelectMenu.ToBuilder());
            }
        }

        return new DiscordBotMessage(await CreateEmbedResponseAsync(map), componentBuilder?.Build());
    }
}