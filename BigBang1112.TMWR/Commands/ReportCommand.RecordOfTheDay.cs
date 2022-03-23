using BigBang1112.DiscordBot;

namespace BigBang1112.TMWR.Commands;

public partial class ReportCommand
{
    [DiscordBotSubCommand("recordoftheday", "Use the bot to report next day's record of the day.")]
    public class RecordOfTheDay : DiscordBotCommand
    {
        public RecordOfTheDay(DiscordBotService discordBotService) : base(discordBotService)
        {

        }
    }
}
