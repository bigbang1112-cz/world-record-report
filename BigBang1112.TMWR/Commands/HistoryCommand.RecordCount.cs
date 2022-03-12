using BigBang1112.Attributes.DiscordBot;
using BigBang1112.WorldRecordReportLib.Repos;

namespace BigBang1112.TMWR.Commands;

public partial class HistoryCommand
{
    [DiscordBotSubCommand("recordcount")]
    public partial class RecordCount : MapRelatedWithUidCommand
    {
        public RecordCount(TmwrDiscordBotService tmwrDiscordBotService, IWrRepo repo) : base(tmwrDiscordBotService, repo)
        {

        }
    }
}
