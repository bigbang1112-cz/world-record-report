namespace BigBang1112.WorldRecordReportLib.TMWR.Commands;

public partial class HistoryCommand
{
    public partial class RecordCount
    {
        [DiscordBotSubCommand("mapgroup", "Gets the history of the record count increase in a map group.")]
        [UnfinishedDiscordBotCommand]
        public class MapGroup : TmwrCommand
        {
            public MapGroup(TmwrDiscordBotService tmwrDiscordBotService) : base(tmwrDiscordBotService)
            {

            }
        }
    }
}
