using BigBang1112.Attributes;
using BigBang1112.DiscordBot;
using BigBang1112.DiscordBot.Attributes;

namespace BigBang1112.WorldRecordReport.DiscordBot;

[DiscordBot("e0d910ee-c360-4a55-9b6c-b4d50e8c3581", name: "WRR",
    Punchline = "World Record Report Discord Bot",
    Description = "Handler of events happening within the World Record Report project.",
    GitRepoUrl = "https://github.com/bigbang1112-cz/world-record-report")]
[SecretAppsettingsPath("DiscordBots:WRR:Secret")]
public class WrrDiscordBotService : DiscordBotService
{
    public WrrDiscordBotService(IServiceProvider serviceProvider) : base(serviceProvider)
    {

    }
}
