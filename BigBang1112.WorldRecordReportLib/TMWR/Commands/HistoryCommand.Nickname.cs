using Discord;

namespace BigBang1112.WorldRecordReportLib.TMWR.Commands;

public partial class HistoryCommand
{
    [DiscordBotSubCommand("nickname")]
    public class Nickname : IdentifyBaseCommand
    {
        public Nickname(TmwrDiscordBotService tmwrDiscordBotService) : base(tmwrDiscordBotService)
        {
            
        }
    }
}
