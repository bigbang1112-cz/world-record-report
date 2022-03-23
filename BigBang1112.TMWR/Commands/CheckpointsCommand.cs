namespace BigBang1112.TMWR.Commands;

[DiscordBotCommand("checkpoints")]
[UnfinishedDiscordBotCommand]
public partial class CheckpointsCommand : DiscordBotCommand
{
    public CheckpointsCommand(TmwrDiscordBotService tmwrDiscordBotService) : base(tmwrDiscordBotService)
    {

    }

    public override Task<DiscordBotMessage> ExecuteAsync(SocketInteraction slashCommand)
    {
        throw new NotImplementedException();
    }
}
