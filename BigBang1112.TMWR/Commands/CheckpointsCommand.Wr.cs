using BigBang1112.Attributes.DiscordBot;
using BigBang1112.WorldRecordReportLib.Repos;

namespace BigBang1112.TMWR.Commands;

public partial class CheckpointsCommand
{
    [DiscordBotSubCommand("wr")]
    public class Wr : MapRelatedWithUidCommand
    {
        public Wr(TmwrDiscordBotService tmwrDiscordBotService, IWrRepo repo) : base(tmwrDiscordBotService, repo)
        {

        }
    }
}
