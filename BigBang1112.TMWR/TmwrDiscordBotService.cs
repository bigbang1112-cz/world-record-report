using BigBang1112.DiscordBot;
using BigBang1112.Attributes;

namespace BigBang1112.TMWR;

[DiscordBot("e7593b6b-d8f1-4caa-b950-01a8437662d0", name: "TMWR")]
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

    protected override async Task SlashCommandExecutedAsync(SocketSlashCommand slashCommand)
    {
        if (slashCommand.IsDMInteraction)
        {
            await slashCommand.RespondAsync("DM interactions are temporarily disabled.");
            return;
        }

        await base.SlashCommandExecutedAsync(slashCommand);
    }
}
