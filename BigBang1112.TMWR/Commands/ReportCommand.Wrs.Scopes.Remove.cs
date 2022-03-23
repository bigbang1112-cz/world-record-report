using BigBang1112.DiscordBot;

namespace BigBang1112.TMWR.Commands;

public partial class ReportCommand
{
    public partial class Wrs
    {
        public partial class Scopes
        {
            [DiscordBotSubCommand("remove")]
            [UnfinishedDiscordBotCommand]
            public class Remove : DiscordBotCommand
            {
                public Remove(DiscordBotService discordBotService) : base(discordBotService)
                {

                }
            }
        }
    }
}
