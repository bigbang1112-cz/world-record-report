using BigBang1112.DiscordBot.Attributes;
using BigBang1112.DiscordBot.Models;
using Discord.WebSocket;

namespace BigBang1112.TMWR.Commands;

public partial class CheckpointsCommand
{
    [DiscordBotSubCommand("record")]
    public class Record : DiscordBotCommand
    {
        public Record(TmwrDiscordBotService tmwrDiscordBotService) : base(tmwrDiscordBotService)
        {

        }

        public override Task<DiscordBotMessage> ExecuteAsync(SocketSlashCommand slashCommand)
        {
            throw new NotImplementedException();
        }
    }
}
