using BigBang1112.DiscordBot;

namespace BigBang1112.TMWR.Commands;

[DiscordBotCommand("history")]
public partial class HistoryCommand : DiscordBotCommand
{
    public HistoryCommand(DiscordBotService discordBotService) : base(discordBotService)
    {

    }

    public override Task<DiscordBotMessage> ExecuteAsync(SocketSlashCommand slashCommand)
    {
        throw new NotImplementedException();
    }
}
