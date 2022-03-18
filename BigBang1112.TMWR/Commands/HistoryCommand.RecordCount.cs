using BigBang1112.DiscordBot.Attributes;
using BigBang1112.WorldRecordReportLib.Repos;

namespace BigBang1112.TMWR.Commands;

public partial class HistoryCommand
{
    [DiscordBotSubCommand("recordcount")]
    public partial class RecordCount : DiscordBotCommand
    {
        public RecordCount(TmwrDiscordBotService tmwrDiscordBotService) : base(tmwrDiscordBotService)
        {

        }
    }
}
