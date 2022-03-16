using Discord.WebSocket;

namespace BigBang1112.TMWR.Commands;

[DiscordBotCommand("checkpoints")]
public partial class CheckpointsCommand : DiscordBotCommand
{
    public CheckpointsCommand(TmwrDiscordBotService tmwrDiscordBotService) : base(tmwrDiscordBotService)
    {

    }

    public override Task<DiscordBotMessage> ExecuteAsync(SocketSlashCommand slashCommand)
    {
        throw new NotImplementedException();
    }
}
