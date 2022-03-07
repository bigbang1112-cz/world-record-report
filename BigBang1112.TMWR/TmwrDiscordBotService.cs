using BigBang1112.Services;
using BigBang1112.Attributes;
using Discord.WebSocket;
using Discord;

namespace BigBang1112.TMWR;

[SecretAppsettingsPath("DiscordBots:TMWR:Secret")]
public class TmwrDiscordBotService : DiscordBotService
{
    public TmwrDiscordBotService(IServiceProvider serviceProvider) : base(serviceProvider)
    {

    }
}
