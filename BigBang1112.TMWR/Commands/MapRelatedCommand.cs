using BigBang1112.Attributes.DiscordBot;
using BigBang1112.Data;
using BigBang1112.Models;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;
using Discord;
using Discord.WebSocket;
using System.Reflection;

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

    public virtual IEnumerable<SlashCommandOptionBuilder> YieldOptions()
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
    }

    protected static SlashCommandOptionBuilder CreateMapUidOption()
    {
        return new SlashCommandOptionBuilder
        {
            Name = OptionMapUid,
            Type = ApplicationCommandOptionType.String,
            Description = "Map UID of the map.",
            IsAutocomplete = true
        };
    }

    public virtual async Task<DiscordBotMessage> ExecuteAsync(SocketSlashCommand slashCommand, IEnumerable<SocketSlashCommandDataOption> options)
    {
        var mapName = default(string);
        var env = default(string);
        var title = default(string);

        foreach (var option in options)
        {
            switch (option.Name)
            {
                case OptionMapName:
                    mapName = (string)option.Value;
                    break;
                case OptionEnv:
                    env = (string)option.Value;
                    break;
                case OptionTitle:
                    title = (string)option.Value;
                    break;
                case OptionMapUid:
                    var mapUid = (string)option.Value;
                    var map = await _repo.GetMapByUidAsync(mapUid);

                    if (map is null)
                    {
                        break;
                    }

                    return await CreateResponseMessageWithMapsParamAsync(Enumerable.Repeat(map, 1), options);
            }
        }

        // If only title would be specified, then for the top 10 command it could display top 10 title ranking, once poss

        var maps = await _repo.GetMapsByMultipleParamsAsync(mapName, env, title);

        return maps.Any()
            ? await CreateResponseMessageWithMapsParamAsync(maps, options)
            : CreateResponseMessageNoMapsFound();
    }

    public virtual async Task<DiscordBotMessage> ExecuteAsync(SocketSlashCommand slashCommand)
    {
        var subCommand = slashCommand.Data.Options.FirstOrDefault(x => x.Type == ApplicationCommandOptionType.SubCommand);

        return subCommand is null
            ? await ExecuteAsync(slashCommand, slashCommand.Data.Options)
            : await ExecuteAsync(slashCommand, subCommand.Options);
    }

    protected static DiscordBotMessage CreateResponseMessageNoMapsFound()
    {
        var embed = new EmbedBuilder()
            .WithTitle("No maps were found")
            .WithDescription("Try different set of filters.")
            .Build();

        return new DiscordBotMessage(embed, ephemeral: true);
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

    public virtual async Task<DiscordBotMessage> CreateResponseMessageWithMapsParamAsync(IEnumerable<MapModel> mapsForMenu, IEnumerable<SocketSlashCommandDataOption> options)
    {
        var map = mapsForMenu.First();

        var embed = await CreateEmbedResponseAsync(map);

        var builder = await CreateComponentsAsync(map, options);

        if (mapsForMenu.Count() > 1)
        {
            var lookup = mapsForMenu.ToLookup(x => x.DeformattedName);

            var customId = GetType().GetCustomAttribute<DiscordBotCommandAttribute>()?.Name.Replace(' ', '_') ?? throw new Exception();

            builder ??= new ComponentBuilder();
            builder.WithSelectMenu(CreateSelectMenu(customId, mapsForMenu, lookup));
        }

        return new DiscordBotMessage(embed, builder?.Build());
    }

    protected virtual Task<ComponentBuilder?> CreateComponentsAsync(MapModel map, IEnumerable<SocketSlashCommandDataOption> options)
    {
        return Task.FromResult<ComponentBuilder?>(null);
    }

    protected virtual async Task<Embed> CreateEmbedResponseAsync(MapModel map)
    {
        var builder = new EmbedBuilder();

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

    public virtual async Task<DiscordBotMessage> SelectMenuAsync(SocketMessageComponent messageComponent, IReadOnlyCollection<string> values)
    {
        var mapUid = values.First();

        var map = await _repo.GetMapByUidAsync(mapUid);

        if (map is null)
        {
            return new DiscordBotMessage(new EmbedBuilder().WithTitle("Map not found.").Build());
        }

        return new DiscordBotMessage(await CreateEmbedResponseAsync(map));
    }
}
