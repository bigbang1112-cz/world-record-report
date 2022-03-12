using BigBang1112.Attributes.DiscordBot;
using BigBang1112.Models.DiscordBot;
using BigBang1112.Services;
using Discord.WebSocket;

namespace BigBang1112.TMWR.Commands;

[DiscordBotCommand("compare")]
public partial class CompareCommand : DiscordBotCommand
{
    public CompareCommand(TmwrDiscordBotService tmwrDiscordBotService) : base(tmwrDiscordBotService)
    {

    }

    public override Task<DiscordBotMessage> ExecuteAsync(SocketSlashCommand slashCommand)
    {
        throw new NotImplementedException();
    }
}
