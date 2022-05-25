namespace BigBang1112.WorldRecordReportLib.TMWR.Commands;

public partial class HistoryCommand
{
    [DiscordBotSubCommand("recordcount")]
    [UnfinishedDiscordBotCommand]
    public partial class RecordCount : TmwrCommand
    {
        public RecordCount(TmwrDiscordBotService tmwrDiscordBotService) : base(tmwrDiscordBotService)
        {

        }
    }
}
