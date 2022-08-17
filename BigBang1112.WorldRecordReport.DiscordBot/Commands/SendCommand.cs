using BigBang1112.DiscordBot;
using BigBang1112.DiscordBot.Attributes;
using BigBang1112.DiscordBot.Models;

namespace BigBang1112.WorldRecordReport.DiscordBot.Commands;

[DiscordBotCommand("send", "Sends something.")]
public partial class SendCommand : DiscordBotCommand
{
    public SendCommand(DiscordBotService discordBotService) : base(discordBotService)
    {

    }
}
