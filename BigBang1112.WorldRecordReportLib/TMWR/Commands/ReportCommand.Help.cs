namespace BigBang1112.WorldRecordReportLib.TMWR.Commands;

public partial class ReportCommand
{
    [DiscordBotSubCommand("help", "Gives help related to reporting using the bot.")]
    public class Help : TmwrCommand
    {
        public Help(TmwrDiscordBotService tmwrDiscordBotService) : base(tmwrDiscordBotService)
        {
            
        }
    }
}
