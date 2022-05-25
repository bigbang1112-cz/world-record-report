namespace BigBang1112.WorldRecordReportLib.TMWR.Commands;

[DiscordBotCommand("wroftheweek", "Shows the current world record of the week.")]
[UnfinishedDiscordBotCommand]
public class WrOfTheWeekCommand : TmwrCommand
{
    public WrOfTheWeekCommand(TmwrDiscordBotService tmwrDiscordBotService) : base(tmwrDiscordBotService)
    {

    }
}
