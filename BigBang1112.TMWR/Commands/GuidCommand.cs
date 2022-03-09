﻿using BigBang1112.Attributes.DiscordBot;
using BigBang1112.Models;
using Discord;
using Discord.WebSocket;

namespace BigBang1112.TMWR.Commands;

[DiscordBotCommand("guid", "Gets the UUID of an entity.")]
public class GuidCommand : IDiscordBotCommand
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
