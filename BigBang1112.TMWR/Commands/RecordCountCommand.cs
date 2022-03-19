using BigBang1112.DiscordBot;

namespace BigBang1112.TMWR.Commands;

[DiscordBotCommand("recordcount")]
public partial class RecordCountCommand : DiscordBotCommand
{
    public RecordCountCommand(DiscordBotService discordBotService) : base(discordBotService)
    {
        
    }

    public override Task<DiscordBotMessage> ExecuteAsync(SocketInteraction slashCommand)
    {
        throw new NotImplementedException();
    }
}
