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
