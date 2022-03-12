using BigBang1112.Attributes.DiscordBot;
using BigBang1112.Models.DiscordBot;
using Discord.WebSocket;

namespace BigBang1112.TMWR.Commands;

[DiscordBotCommand("recordoftheday")]
public class RecordOfTheDayCommand : DiscordBotCommand
{
    public RecordOfTheDayCommand(TmwrDiscordBotService tmwrDiscordBotService) : base(tmwrDiscordBotService)
    {

    }

    public override Task<DiscordBotMessage> ExecuteAsync(SocketSlashCommand slashCommand)
    {
        throw new NotImplementedException();
    }
}
