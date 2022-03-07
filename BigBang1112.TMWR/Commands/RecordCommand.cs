using BigBang1112.Attributes.DiscordBot;
using BigBang1112.Models;
using Discord;
using Discord.WebSocket;

namespace BigBang1112.TMWR.Commands;

[DiscordBotCommand("record", "Shows information about certain record")]
public class RecordCommand : IDiscordBotCommand
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
        yield return new SlashCommandOptionBuilder
        {
            Name = "tmx",
            Type = ApplicationCommandOptionType.SubCommand,
            Options = new List<SlashCommandOptionBuilder> {
                new SlashCommandOptionBuilder
                {
                    Name = "value",
                    Type = ApplicationCommandOptionType.String
                }
            }
        };

        yield return new SlashCommandOptionBuilder
        {
            Name = "official",
            Type = ApplicationCommandOptionType.SubCommand,
            Options = new List<SlashCommandOptionBuilder> {
                new SlashCommandOptionBuilder
                {
                    Name = "value",
                    Type = ApplicationCommandOptionType.String
                }
            }
        };
    }
}
