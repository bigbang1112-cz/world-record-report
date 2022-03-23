using BigBang1112.DiscordBot;

namespace BigBang1112.TMWR.Commands;

[DiscordBotCommand("report")]
[UnfinishedDiscordBotCommand]
public partial class ReportCommand : DiscordBotCommand
{
    public ReportCommand(DiscordBotService discordBotService) : base(discordBotService)
    {

    }
}
