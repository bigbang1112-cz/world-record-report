using BigBang1112.Attributes.DiscordBot;
using BigBang1112.Models.DiscordBot;
using Discord.WebSocket;

namespace BigBang1112.TMWR.Commands;

public partial class CompareCommand
{
    [DiscordBotSubCommand("records")]
    public class Records : DiscordBotCommand
    {
        public Records(TmwrDiscordBotService tmwrDiscordBotService) : base(tmwrDiscordBotService)
        {

        }

        public override Task<DiscordBotMessage> ExecuteAsync(SocketSlashCommand slashCommand)
        {
            throw new NotImplementedException();
        }
    }
}
