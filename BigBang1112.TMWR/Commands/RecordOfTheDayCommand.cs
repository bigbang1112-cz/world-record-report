namespace BigBang1112.TMWR.Commands;

[DiscordBotCommand("recordoftheday")]
public class RecordOfTheDayCommand : DiscordBotCommand
{
    public RecordOfTheDayCommand(TmwrDiscordBotService tmwrDiscordBotService) : base(tmwrDiscordBotService)
    {

    }

    public override Task<DiscordBotMessage> ExecuteAsync(SocketInteraction slashCommand)
    {
        throw new NotImplementedException();
    }
}
