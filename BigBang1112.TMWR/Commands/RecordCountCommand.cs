using BigBang1112.Attributes.DiscordBot;
using BigBang1112.Models;
using Discord;
using Discord.WebSocket;

namespace BigBang1112.TMWR.Commands;

[DiscordBotCommand("recordcount", "Shows the amount of records on a map.")]
public class RecordCountCommand : IDiscordBotCommand
{
    public Task AutocompleteAsync(SocketAutocompleteInteraction interaction, AutocompleteOption option)
    {
        return Task.CompletedTask;
    }

    public Task ExecuteAsync(SocketSlashCommand slashCommand)
    {
        return Task.CompletedTask;
    }

    public Task SelectMenuAsync(SocketMessageComponent messageComponent, IReadOnlyCollection<string> values)
    {
        return Task.CompletedTask;
    }

    public IEnumerable<SlashCommandOptionBuilder> YieldOptions()
    {
        yield break;
    }
}
