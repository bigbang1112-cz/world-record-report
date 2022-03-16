using Discord.WebSocket;

namespace BigBang1112.TMWR.Commands;

public partial class HistoryCommand
{
    public partial class RecordCount
    {
        [DiscordBotSubCommand("campaign")]
        public class Campaign : DiscordBotCommand
        {
            public Campaign(TmwrDiscordBotService tmwrDiscordBotService) : base(tmwrDiscordBotService)
            {

            }

            public override Task<DiscordBotMessage> ExecuteAsync(SocketSlashCommand slashCommand)
            {
                throw new NotImplementedException();
            }
        }
    }
}
