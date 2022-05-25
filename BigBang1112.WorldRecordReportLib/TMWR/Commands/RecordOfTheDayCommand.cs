namespace BigBang1112.WorldRecordReportLib.TMWR.Commands;

[DiscordBotCommand("recordoftheday", "Shows the current record of the day.")]
[UnfinishedDiscordBotCommand]
public class RecordOfTheDayCommand : TmwrCommand
{
    public RecordOfTheDayCommand(TmwrDiscordBotService tmwrDiscordBotService) : base(tmwrDiscordBotService)
    {

    }
}
