using BigBang1112.DiscordBot;

namespace BigBang1112.TMWR.Commands;

public partial class ReportCommand
{
    [DiscordBotSubCommand("wroftheweek", "Use the bot to report next week's world record of the week.")]
    public class WrOfTheWeek : DiscordBotCommand
    {
        public WrOfTheWeek(DiscordBotService discordBotService) : base(discordBotService)
        {

        }
    }
}
