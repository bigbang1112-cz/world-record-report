namespace BigBang1112.TMWR.Commands;

[DiscordBotCommand("inputs")]
[UnfinishedDiscordBotCommand]
public partial class InputsCommand : DiscordBotCommand
{
    public InputsCommand(TmwrDiscordBotService tmwrDiscordBotService) : base(tmwrDiscordBotService)
    {

    }

    public override Task<DiscordBotMessage> ExecuteAsync(SocketInteraction slashCommand)
    {
        throw new NotImplementedException();
    }
}
