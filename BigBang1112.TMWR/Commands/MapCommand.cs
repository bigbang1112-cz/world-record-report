using BigBang1112.DiscordBot;

namespace BigBang1112.TMWR.Commands;

[DiscordBotCommand("map")]
public partial class MapCommand : DiscordBotCommand
{
    public MapCommand(DiscordBotService discordBotService) : base(discordBotService)
    {

    }

    public override Task<DiscordBotMessage> ExecuteAsync(SocketSlashCommand slashCommand)
    {
        throw new NotImplementedException();
    }
}
