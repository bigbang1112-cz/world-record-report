using Discord;
using Discord.WebSocket;

namespace BigBang1112.WorldRecordReport.Services;

public class DiscordBotService : IHostedService
{
    private readonly IConfiguration config;
    private readonly DiscordSocketClient client;

    public DiscordBotService(IConfiguration config)
    {
        this.config = config;

        client = new DiscordSocketClient();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var secret = config["DiscordBots:TMWR:Secret"];

        if (string.IsNullOrWhiteSpace(secret))
        {
            return;
        }

        await client.LoginAsync(TokenType.Bot, secret);
        await client.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await client.StopAsync();
    }
}
