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
