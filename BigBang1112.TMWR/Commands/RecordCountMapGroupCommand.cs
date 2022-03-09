using BigBang1112.Attributes.DiscordBot;
using BigBang1112.Models;
using Discord;
using Discord.WebSocket;

namespace BigBang1112.TMWR.Commands;

[DiscordBotCommand("recordcount mapgroup", "Shows the amount of records on each map in map group plus the map group overall.")]
public class RecordCountMapGroupCommand : IDiscordBotCommand
{
    public Task AutocompleteAsync(SocketAutocompleteInteraction interaction, AutocompleteOption option)
    {
        return Task.CompletedTask;
    }

    public Task<DiscordBotMessage> ExecuteAsync(SocketSlashCommand slashCommand)
    {
        throw new NotImplementedException();
    }

    public Task<DiscordBotMessage> SelectMenuAsync(SocketMessageComponent messageComponent, IReadOnlyCollection<string> values)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<SlashCommandOptionBuilder> YieldOptions()
    {
        yield break;
    }
}
