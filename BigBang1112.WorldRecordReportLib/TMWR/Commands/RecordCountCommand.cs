using BigBang1112.DiscordBot;
using BigBang1112.DiscordBot.Models;
using Discord.WebSocket;

namespace BigBang1112.WorldRecordReportLib.TMWR.Commands;

[DiscordBotCommand("recordcount")]
public partial class RecordCountCommand : TmwrCommand
{
    public RecordCountCommand(TmwrDiscordBotService tmwrDiscordBotService) : base(tmwrDiscordBotService)
    {
        
    }

    public override Task<DiscordBotMessage> ExecuteAsync(SocketInteraction slashCommand)
    {
        throw new NotImplementedException();
    }
}
