using BigBang1112.Attributes.DiscordBot;
using BigBang1112.WorldRecordReportLib.Repos;

namespace BigBang1112.TMWR.Commands;

public partial class HistoryCommand
{
    public partial class RecordCount
    {
        [DiscordBotSubCommand("map")]
        public class Map : MapRelatedWithUidCommand
        {
            public Map(TmwrDiscordBotService tmwrDiscordBotService, IWrRepo repo) : base(tmwrDiscordBotService, repo)
            {

            }
        }
    }
}
