namespace BigBang1112.TMWR.Commands;

[DiscordBotCommand("wroftheweek", "Shows the current world record of the week.")]
[UnfinishedDiscordBotCommand]
public class WrOfTheWeekCommand : DiscordBotCommand
{
    public WrOfTheWeekCommand(TmwrDiscordBotService tmwrDiscordBotService) : base(tmwrDiscordBotService)
    {

    }

    public override Task<DiscordBotMessage> ExecuteAsync(SocketInteraction slashCommand)
    {
        throw new NotImplementedException();
    }
}
