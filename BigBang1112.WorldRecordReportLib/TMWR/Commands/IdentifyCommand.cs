using BigBang1112.DiscordBot.Models;

namespace BigBang1112.WorldRecordReportLib.TMWR.Commands;

[DiscordBotCommand("identify")]
public class IdentifyCommand : IdentifyBaseCommand
{
    public IdentifyCommand(TmwrDiscordBotService tmwrDiscordBotService) : base(tmwrDiscordBotService)
    {
        
    }
}
