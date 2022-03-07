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

    public MapRelatedCommand(IWrRepo repo)
    {
        _repo = repo;
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

    public virtual Task ExecuteWithMapsAsync(SocketSlashCommand slashCommand, List<MapModel> maps)
    {
        return Task.CompletedTask;
    }

    public async Task AutocompleteAsync(SocketAutocompleteInteraction interaction, AutocompleteOption option)
    {
        switch (option.Name)
        {
            case OptionMapName:
                await interaction.RespondAsync(await AutocompleteAsync(_repo.GetMapNamesAsync, option.Value));
                break;
            case OptionEnv:
                await interaction.RespondAsync(await AutocompleteAsync(_repo.GetEnvNamesAsync, option.Value));
                break;
            case OptionMapUid:
                await interaction.RespondAsync(await AutocompleteAsync(_repo.GetMapUidsAsync, option.Value));
                break;
            case OptionTitle:
                await interaction.RespondAsync(await AutocompleteAsync(_repo.GetTitlePacksAsync, option.Value));
                break;
        }
    }

    private static async Task<IEnumerable<AutocompleteResult>> AutocompleteAsync(Func<string, int, CancellationToken, Task<List<string>>> func, object value)
    {
        return (await func.Invoke(value.ToString() ?? "", DiscordConsts.OptionLimit, default))
            .Select(x => new AutocompleteResult(x, x));
    }

    public static IEnumerable<(string mapName, MapModel map)> GetUniqueMapNames(IEnumerable<MapModel> maps)
    {
        var set = new HashSet<string>();

        foreach (var map in maps)
        {
            var counter = 0;
            var hasDupe = set.Contains(map.Name);

            if (!hasDupe)
            {
                foreach (var otherMap in maps)
                {
                    if (map.Name == otherMap.Name)
                    {
                        counter++;
                    }

                    if (counter >= 2)
                    {
                        hasDupe = true;
                        set.Add(map.Name);
                        break;
                    }
                }
            }

            yield return hasDupe
                ? ($"{map.DeformattedName ?? map.Name} ({map.Environment})", map)
                : (map.DeformattedName ?? map.Name, map);
        }
    }

    public virtual Task SelectMenuAsync(SocketMessageComponent messageComponent, IReadOnlyCollection<string> values)
    {
        return Task.CompletedTask;
    }
}
