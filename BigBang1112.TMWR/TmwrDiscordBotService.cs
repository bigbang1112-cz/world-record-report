using BigBang1112.DiscordBot;
using BigBang1112.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace BigBang1112.TMWR;

[DiscordBot("e7593b6b-d8f1-4caa-b950-01a8437662d0", name: "TMWR",
    Punchline = "The Ultimate Trackmania World Record Discord Bot",
    Description = "With this bot, you can quickly check any world records, history of world records, graphs, or replay parameters in the future.",
    GitRepoUrl = "https://github.com/bigbang1112-cz/world-record-report")]
[SecretAppsettingsPath("DiscordBots:TMWR:Secret")]
public class TmwrDiscordBotService : DiscordBotService
{
    private readonly IConfiguration _config;

    public TmwrDiscordBotService(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _config = serviceProvider.GetRequiredService<IConfiguration>();
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
        if (_config.GetValue<bool>("DiscordBotDisableDMs") && slashCommand.IsDMInteraction && slashCommand.User.Id != GetOwnerDiscordSnowflake())
        {
            await slashCommand.RespondAsync("DM interactions are temporarily disabled.");
            return;
        }

        await base.SlashCommandExecutedAsync(slashCommand);
    }
}
