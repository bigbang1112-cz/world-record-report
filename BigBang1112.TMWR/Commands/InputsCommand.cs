using BigBang1112.Attributes.DiscordBot;
using BigBang1112.Models.DiscordBot;
using Discord.WebSocket;

namespace BigBang1112.TMWR.Commands;

[DiscordBotCommand("inputs")]
public partial class InputsCommand : DiscordBotCommand
{
    public InputsCommand(TmwrDiscordBotService tmwrDiscordBotService) : base(tmwrDiscordBotService)
    {

    }

    public override Task<DiscordBotMessage> ExecuteAsync(SocketSlashCommand slashCommand)
    {
        throw new NotImplementedException();
    }
}
