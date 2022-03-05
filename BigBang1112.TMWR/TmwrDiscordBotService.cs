using BigBang1112.Services;
using BigBang1112.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Discord;
using Discord.Net;

namespace BigBang1112.TMWR;

[SecretAppsettingsPath("DiscordBots:TMWR:Secret")]
public class TmwrDiscordBotService : DiscordBotService
{
    private readonly ILogger<TmwrDiscordBotService> _logger;

    public TmwrDiscordBotService(IConfiguration config, ILogger<TmwrDiscordBotService> logger) : base(config, logger)
    {
        _logger = logger;
    }

    protected override async Task Ready()
    {
        //await Client.BulkOverwriteGlobalApplicationCommandsAsync();

        var globalCommand = new SlashCommandBuilder()
            .WithName("top10")
            .WithDescription("Shows the Top 10 world leaderboard.");

        var guild = Client.Guilds.First();

        try
        {
            
        }
        catch (HttpException exception)
        {
            _logger.LogError(exception, "Error when creating slash command.");
        }
    }
}
