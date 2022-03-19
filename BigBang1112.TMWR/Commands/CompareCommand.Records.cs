namespace BigBang1112.TMWR.Commands;

public partial class CompareCommand
{
    [DiscordBotSubCommand("records")]
    public class Records : DiscordBotCommand
    {
        public Records(TmwrDiscordBotService tmwrDiscordBotService) : base(tmwrDiscordBotService)
        {

        }

        public override Task<DiscordBotMessage> ExecuteAsync(SocketInteraction slashCommand)
        {
            throw new NotImplementedException();
        }
    }
}
