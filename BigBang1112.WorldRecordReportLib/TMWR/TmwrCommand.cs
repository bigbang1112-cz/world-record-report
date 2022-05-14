using BigBang1112.DiscordBot.Models;

namespace BigBang1112.WorldRecordReportLib.TMWR;

public abstract class TmwrCommand : DiscordBotCommand
{
    public TmwrCommand(TmwrDiscordBotService tmwrDiscordBotService) : base(tmwrDiscordBotService)
    {
    }
}
