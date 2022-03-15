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

    protected override async Task ReadyAsync()
    {
        await base.ReadyAsync();

        var botVersion = typeof(TmwrDiscordBotService).Assembly.GetName().Version;
        var wrrlibversion = typeof(WorldRecordReportLib.Data.WrContext).Assembly.GetName().Version;

        await Client.SetGameAsync($"{botVersion?.ToString() ?? "unknown version"} (WrrLib: {wrrlibversion?.ToString() ?? "unknown version"})");
    }
}
