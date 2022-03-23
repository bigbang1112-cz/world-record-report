using BigBang1112.DiscordBot;

namespace BigBang1112.TMWR.Commands;

public partial class ReportCommand
{
    [DiscordBotSubCommand("wrs", "Use the bot to report new world records.")]
    public class Wrs : DiscordBotCommand
    {
        public Wrs(DiscordBotService discordBotService) : base(discordBotService)
        {

        }
    }
}
