using BigBang1112.DiscordBot.Models;
using Discord.WebSocket;

namespace BigBang1112.WorldRecordReportLib.TMWR.Commands;

public partial class InputsCommand
{
    [DiscordBotSubCommand("record")]
    [UnfinishedDiscordBotCommand]
    public class Record : TmwrCommand
    {
        public Record(TmwrDiscordBotService tmwrDiscordBotService) : base(tmwrDiscordBotService)
        {

        }

        public override Task<DiscordBotMessage> ExecuteAsync(SocketInteraction slashCommand)
        {
            throw new NotImplementedException();
        }
    }
}
