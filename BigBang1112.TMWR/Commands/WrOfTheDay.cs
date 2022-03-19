namespace BigBang1112.TMWR.Commands;

[DiscordBotCommand("wroftheday")]
public class WrOfTheDayCommand : DiscordBotCommand
{
    public WrOfTheDayCommand(TmwrDiscordBotService tmwrDiscordBotService) : base(tmwrDiscordBotService)
    {

    }

    public override Task<DiscordBotMessage> ExecuteAsync(SocketInteraction slashCommand)
    {
        throw new NotImplementedException();
    }
}
