using BigBang1112.WorldRecordReportLib.Data;
using BigBang1112.WorldRecordReportLib.Repos;

namespace BigBang1112.TMWR.Commands;

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
