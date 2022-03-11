using BigBang1112.Attributes.DiscordBot;
using BigBang1112.Models.DiscordBot;
using BigBang1112.Services;
using Discord.WebSocket;

namespace BigBang1112.TMWR.Commands;

[DiscordBotCommand("history")]
public partial class HistoryCommand : DiscordBotCommand
{
    public HistoryCommand(DiscordBotService discordBotService) : base(discordBotService)
    {

    }

    public override Task<DiscordBotMessage> ExecuteAsync(SocketSlashCommand slashCommand)
    {
        throw new NotImplementedException();
    }
}
