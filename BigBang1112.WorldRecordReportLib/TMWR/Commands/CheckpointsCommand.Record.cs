namespace BigBang1112.WorldRecordReportLib.TMWR.Commands;

public partial class CheckpointsCommand
{
    [DiscordBotSubCommand("record")]
    [UnfinishedDiscordBotCommand]
    public class Record : TmwrCommand
    {
        public Record(TmwrDiscordBotService tmwrDiscordBotService) : base(tmwrDiscordBotService)
        {

        }
    }
}
