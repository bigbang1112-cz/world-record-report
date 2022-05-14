namespace BigBang1112.WorldRecordReportLib.TMWR.Commands;

public partial class HistoryCommand
{
    public partial class RecordCount
    {
        [DiscordBotSubCommand("campaign")]
        [UnfinishedDiscordBotCommand]
        public class Campaign : TmwrCommand
        {
            public Campaign(TmwrDiscordBotService tmwrDiscordBotService) : base(tmwrDiscordBotService)
            {

            }

        }
    }
}
