using BigBang1112.Data;
using BigBang1112.Models;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;
using Discord;
using Discord.WebSocket;

namespace BigBang1112.TMWR.Commands;

public abstract class MapRelatedCommand : IDiscordBotCommand
{
    public const string OptionMapName = "name";
    public const string OptionMapUid = "uid";
    public const string OptionEnv = "env";
    public const string OptionTitle = "title";

    private readonly IWrRepo _repo;

    protected Dictionary<string, Func<string, int, CancellationToken, Task<List<string>>>> AutocompleteOptions { get; }

    public MapRelatedCommand(IWrRepo repo)
    {
        _repo = repo;

        AutocompleteOptions = new()
        {
            { OptionMapName, _repo.GetMapNamesAsync },
            { OptionEnv, _repo.GetEnvNamesAsync },
            { OptionMapUid, _repo.GetMapUidsAsync },
            { OptionTitle, _repo.GetTitlePacksAsync }
        };
    }

    public IEnumerable<SlashCommandOptionBuilder> YieldOptions()
    {
        yield return new SlashCommandOptionBuilder
        {
            Name = OptionMapName,
            Type = ApplicationCommandOptionType.String,
            Description = "Name of the map.",
            IsAutocomplete = true
        };

        yield return new SlashCommandOptionBuilder
        {
            Name = OptionEnv,
            Type = ApplicationCommandOptionType.String,
            Description = "Environment of the map.",
            IsAutocomplete = true
        };

        yield return new SlashCommandOptionBuilder
        {
            Name = OptionTitle,
            Type = ApplicationCommandOptionType.String,
            Description = "Title pack of the map.",
            IsAutocomplete = true
        };

        yield return new SlashCommandOptionBuilder
        {
            Name = OptionMapUid,
            Type = ApplicationCommandOptionType.String,
            Description = "Map UID of the map.",
            IsAutocomplete = true
        };
    }

    public virtual async Task ExecuteAsync(SocketSlashCommand slashCommand)
    {
        var mapName = default(string);

        foreach (var option in slashCommand.Data.Options)
        {
            switch (option.Name)
            {
                case OptionMapName:
                    mapName = (string)option.Value;
                    break;
                case OptionMapUid:
                    var mapUid = (string)option.Value;
                    var map = await _repo.GetMapByUidAsync(mapUid);

                    if (map is null)
                    {
                        break;
                    }

                    await ExecuteWithMapsAsync(slashCommand, new List<MapModel> { map });

                    return;
            }
        }

        var maps = default(List<MapModel>);

        if (mapName is null)
        {
            await RespondWithNoMapsFound(slashCommand);
            return;
        }

        maps = await _repo.GetMapsByNameAsync(mapName);

        if (!maps.Any())
        {
            await RespondWithNoMapsFound(slashCommand);
            return;
        }

        await ExecuteWithMapsAsync(slashCommand, maps);
    }

    protected static async Task RespondWithNoMapsFound(SocketSlashCommand slashCommand)
    {
        var embed = new EmbedBuilder()
            .WithTitle("No map was found")
            .WithDescription("Try different set of filters.")
            .Build();

        await slashCommand.RespondAsync(embed: embed);
    }

    public async Task AutocompleteAsync(SocketAutocompleteInteraction interaction, AutocompleteOption option)
    {
        if (AutocompleteOptions.TryGetValue(option.Name, out var stringListFunc))
        {
            await interaction.RespondAsync(await AutocompleteOptionAsync(stringListFunc, option.Value));
        }
    }

    private static async Task<IEnumerable<AutocompleteResult>> AutocompleteOptionAsync(Func<string, int, CancellationToken, Task<List<string>>> func, object value)
    {
        return (await func.Invoke(value.ToString() ?? "", DiscordConsts.OptionLimit, default))
            .Select(x => new AutocompleteResult(x, x));
    }

    public virtual async Task ExecuteWithMapsAsync(SocketSlashCommand slashCommand, List<MapModel> mapsForMenu)
    {
        var map = mapsForMenu.First();

        var lookup = mapsForMenu.ToLookup(x => x.DeformattedName);

        var firstMapHasMultipleSameNames = lookup[map.DeformattedName].Count() > 1;

        var embed = await CreateEmbedAsync(map, firstMapHasMultipleSameNames);

        var component = default(MessageComponent);

        if (mapsForMenu.Count > 1)
        {
            component = CreateSelectMenu(slashCommand, mapsForMenu, lookup);
        }

        await slashCommand.RespondAsync("Execution result:", embed: embed, components: component);
    }

    protected virtual Task<Embed> CreateEmbedAsync(MapModel map, bool firstMapHasMultipleSameNames)
    {
        return Task.FromResult(new EmbedBuilder().WithTitle("Map-related command not implemented").Build());
    }

    private static MessageComponent CreateSelectMenu(SocketSlashCommand slashCommand, List<MapModel> mapsForMenu, ILookup<string, MapModel> lookup)
    {
        var menuBuilder = new SelectMenuBuilder()
            .WithPlaceholder("Select other map...")
            .WithCustomId(slashCommand.CommandName);

        foreach (var m in mapsForMenu)
        {
            var deformattedName = m.GetHumanizedDeformattedName();

            var hasMultipleSameNames = lookup[deformattedName].Count() > 1;

            var label = hasMultipleSameNames
                ? $"{deformattedName} [{m.GetTitleUidOrEnvironment()}]"
                : deformattedName;

            menuBuilder.AddOption(label, m.MapUid, description: $"by {m.Author.GetDeformattedNickname()}");
        }

        return new ComponentBuilder()
            .WithSelectMenu(menuBuilder)
            .Build();
    }

    public virtual async Task SelectMenuAsync(SocketMessageComponent messageComponent, IReadOnlyCollection<string> values)
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
}
