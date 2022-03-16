using BigBang1112.DiscordBot.Attributes;
using BigBang1112.DiscordBot.Models;
using Discord.WebSocket;

namespace BigBang1112.TMWR.Commands;

public partial class HistoryCommand
{
    public partial class RecordCount
    {
        [DiscordBotSubCommand("mapgroup")]
        public class MapGroup : DiscordBotCommand
        {
            public MapGroup(TmwrDiscordBotService tmwrDiscordBotService) : base(tmwrDiscordBotService)
            {

            }

            public override Task<DiscordBotMessage> ExecuteAsync(SocketSlashCommand slashCommand)
            {
                throw new NotImplementedException();
            }
        }
    }
}
