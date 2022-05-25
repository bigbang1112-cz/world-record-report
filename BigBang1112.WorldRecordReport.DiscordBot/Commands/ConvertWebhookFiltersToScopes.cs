using System.Text.Json;
using BigBang1112.DiscordBot;
using BigBang1112.DiscordBot.Attributes;
using BigBang1112.DiscordBot.Models;
using BigBang1112.WorldRecordReportLib.Data;
using BigBang1112.WorldRecordReportLib.Models;
using Discord.WebSocket;

namespace BigBang1112.WorldRecordReport.DiscordBot.Commands;

[DiscordBotCommand("convertwebhookfilterstoscopes", "Converts the current webhook filters to scopes.")]
public class ConvertWebhookFiltersToScopes : DiscordBotCommand
{
    private readonly IWrUnitOfWork _wrUnitOfWork;

    public ConvertWebhookFiltersToScopes(DiscordBotService discordBotService, IWrUnitOfWork wrUnitOfWork) : base(discordBotService)
    {
        _wrUnitOfWork = wrUnitOfWork;
    }

    public override async Task<DiscordBotMessage> ExecuteAsync(SocketInteraction slashCommand)
    {
        foreach (var webhook in await _wrUnitOfWork.DiscordWebhooks.GetAllAsync())
        {
            if (webhook.Filter is null)
            {
                continue;
            }

            DiscordWebhookFilter? filter;

            try
            {
                filter = JsonSerializer.Deserialize<DiscordWebhookFilter>(webhook.Filter);
            }
            catch
            {
                continue;
            }

            if (filter is null)
            {
                continue;
            }

            webhook.Filter = null;
            webhook.Scope = filter.ToReportScopeSet();
        }

        await _wrUnitOfWork.SaveAsync();

        return new DiscordBotMessage("Converted webhook filters to scopes.");
    }
}
