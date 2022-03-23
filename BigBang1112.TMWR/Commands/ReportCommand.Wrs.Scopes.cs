using BigBang1112.DiscordBot;

namespace BigBang1112.TMWR.Commands;

public partial class ReportCommand
{
    public partial class Wrs
    {
        [DiscordBotSubCommand("scopes")]
        [UnfinishedDiscordBotCommand]
        public partial class Scopes : DiscordBotCommand
        {
            public Scopes(DiscordBotService discordBotService) : base(discordBotService)
            {

            }
        }
    }
}
