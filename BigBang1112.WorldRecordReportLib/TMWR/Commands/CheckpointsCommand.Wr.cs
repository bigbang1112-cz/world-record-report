using BigBang1112.WorldRecordReportLib.Data;

namespace BigBang1112.WorldRecordReportLib.TMWR.Commands;

public partial class CheckpointsCommand
{
    [DiscordBotSubCommand("wr")]
    [UnfinishedDiscordBotCommand]
    public class Wr : MapRelatedWithUidCommand
    {
        public Wr(TmwrDiscordBotService tmwrDiscordBotService, IWrUnitOfWork wrUnitOfWork) : base(tmwrDiscordBotService, wrUnitOfWork)
        {

        }
    }
}
