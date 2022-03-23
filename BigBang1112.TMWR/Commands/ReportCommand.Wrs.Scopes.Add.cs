using BigBang1112.DiscordBot;

namespace BigBang1112.TMWR.Commands;

public partial class ReportCommand
{
    public partial class Wrs
    {
        public partial class Scopes
        {
            [DiscordBotSubCommand("add")]
            [UnfinishedDiscordBotCommand]
            public class Add : DiscordBotCommand
            {
                public Add(DiscordBotService discordBotService) : base(discordBotService)
                {

                }
            }
        }
    }
}
