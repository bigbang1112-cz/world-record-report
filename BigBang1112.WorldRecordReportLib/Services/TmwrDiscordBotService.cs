using BigBang1112.DiscordBot;
using BigBang1112.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using BigBang1112.DiscordBot.Attributes;
using Discord.WebSocket;
using BigBang1112.DiscordBot.Models;
using System.Reflection;
using Discord;
using BigBang1112.WorldRecordReportLib.TMWR.Commands;
using BigBang1112.DiscordBot.Models.Db;

namespace BigBang1112.WorldRecordReportLib.TMWR;

[DiscordBot("e7593b6b-d8f1-4caa-b950-01a8437662d0", name: "TMWR", version: "2.0.0.0",
    Punchline = "The Ultimate Trackmania World Record Discord Bot",
    Description = "With this bot, you can quickly check any world records, history of world records, graphs, or replay parameters in the future.",
    GitRepoUrl = "https://github.com/bigbang1112-cz/world-record-report")]
[SecretAppsettingsPath("DiscordBots:TMWR:Secret")]
public class TmwrDiscordBotService : DiscordBotService
{
    private readonly IConfiguration _config;
    private readonly IServiceProvider _serviceProvider;

    public TmwrDiscordBotService(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _config = serviceProvider.GetRequiredService<IConfiguration>();
        _serviceProvider = serviceProvider;
    }

    protected override async Task ReadyAsync()
    {
        await base.ReadyAsync();

        var wrrlibversion = typeof(TmwrDiscordBotService).Assembly.GetName().Version;

        await Client.SetGameAsync($"{GetVersion() ?? "unknown version"} (WrrLib: {wrrlibversion?.ToString() ?? "unknown version"})");
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

    protected override async Task<DiscordBotMessage?> ButtonExecutedOnAutomaticMessageAsync(SocketMessageComponent messageComponent, Deferer deferer)
    {
        var split = messageComponent.Data.CustomId.Split('-');

        if (split.Length < 3)
        {
            return new DiscordBotMessage(new EmbedBuilder().WithDescription("Not enough data for the command.").Build(),
                ephemeral: true, alwaysPostAsNewMessage: true);
        }

        var isPrevWrCommand = split[1] == "wr" && split.Length > 3 && split[3] == "prev";

        if (isPrevWrCommand)
        {
            return await ExecutePrevWrButtonAsync(messageComponent, deferer, wrGuidStr: split[2].Replace('_', '-'));
        }

        return split[1] switch
        {
            "wr" => await ExecuteWrDetailsButtonAsync(messageComponent, deferer, wrGuidStr: split[2].Replace('_', '-')),
            "comparewrs" => await ExecuteCompareWrsButtonAsync(messageComponent, deferer, wrGuidStr: split[2].Replace('_', '-')),
            _ => null,
        };
    }

    private async Task<DiscordBotMessage?> ExecuteWrDetailsButtonAsync(SocketMessageComponent messageComponent, Deferer deferer, string wrGuidStr)
    {
        using var scope = CreateCommand(out WrCommand? wrCommand);

        if (wrCommand is null)
        {
            throw new Exception();
        }

        wrCommand.Guid = wrGuidStr;

        var message = await wrCommand.ExecuteAsync(messageComponent, deferer);

        return message with { AlwaysPostAsNewMessage = true, Ephemeral = true };
    }

    private async Task<DiscordBotMessage?> ExecuteWrKindOfButtonAsync(string wrGuidStr,
        Func<WrCommand, WorldRecordModel, Task<DiscordBotMessage?>> func)
    {
        using var scope = CreateCommand(out WrCommand? wrCommand);

        if (wrCommand is null)
        {
            throw new Exception();
        }

        var wrGuid = new Guid(wrGuidStr);

        var wrUnitOfWork = scope!.ServiceProvider.GetRequiredService<IWrUnitOfWork>();

        var wr = await wrUnitOfWork.WorldRecords.GetByGuidAsync(wrGuid);

        if (wr is null)
        {
            return new DiscordBotMessage(new EmbedBuilder().WithDescription("No world record found.").Build(),
                ephemeral: true, alwaysPostAsNewMessage: true);
        }

        return await func(wrCommand, wr);
    }

    private async Task<DiscordBotMessage?> ExecutePrevWrButtonAsync(SocketMessageComponent messageComponent, Deferer deferer, string wrGuidStr)
    {
        return await ExecuteWrKindOfButtonAsync(wrGuidStr,
            async (wrCommand, wr) => await wrCommand.ExecutePrevAsync(messageComponent, deferer, wr));
    }

    private async Task<DiscordBotMessage?> ExecuteCompareWrsButtonAsync(SocketMessageComponent messageComponent, Deferer deferer, string wrGuidStr)
    {
        return await ExecuteWrKindOfButtonAsync(wrGuidStr,
            async (wrCommand, wr) => await wrCommand.ExecuteComparePrevAsync(messageComponent, deferer, wr));
    }
}
