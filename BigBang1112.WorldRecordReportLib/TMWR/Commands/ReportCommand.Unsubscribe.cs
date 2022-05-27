using BigBang1112.DiscordBot;
using BigBang1112.DiscordBot.Data;
using BigBang1112.DiscordBot.Models;
using BigBang1112.DiscordBot.Models.Db;
using BigBang1112.WorldRecordReportLib.Models.ReportScopes;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace BigBang1112.WorldRecordReportLib.TMWR.Commands;

public partial class ReportCommand
{
    [DiscordBotSubCommand("unsubscribe", "Unsubscribes from reports in this channel.")]
    public class Unsubscribe : TmwrCommand
    {
        private readonly IDiscordBotUnitOfWork _discordBotUnitOfWork;

        [DiscordBotCommandOption("scope", ApplicationCommandOptionType.String, "Scope to unsubscribe. You can also use /report scopes remove")]
        public string? Scope { get; set; }

        internal static IEnumerable<string> AutocompleteScope(string value)
        {
            return ReportScopeSet.GetReportScopesLike(value);
        }

        [DiscordBotCommandOption("other", ApplicationCommandOptionType.Channel, "Specify other channel to apply/see the subscription to/of.")]
        public SocketChannel? OtherChannel { get; set; }

        public Unsubscribe(TmwrDiscordBotService tmwrDiscordBotService,
                           IDiscordBotUnitOfWork discordBotUnitOfWork) : base(tmwrDiscordBotService)
        {
            _discordBotUnitOfWork = discordBotUnitOfWork;
        }

        public override async Task<DiscordBotMessage> ExecuteAsync(SocketInteraction slashCommand, Deferer deferer)
        {
            if (OtherChannel is not SocketTextChannel textChannel)
            {
                if (OtherChannel is not null)
                {
                    return Respond(description: "The specified channel is not a text channel.");
                }
                
                if (slashCommand.Channel is not SocketTextChannel guildTextChannel)
                {
                    return Respond(description: "You cannot report to your DMs.");
                }

                textChannel = guildTextChannel;
            }

            if (slashCommand.User is not SocketGuildUser guildUser)
            {
                return Respond(description: "You're not executing the command from a server.");
            }

            if (!guildUser.GuildPermissions.ManageChannels)
            {
                return Respond(description: $"You don't have permissions to unsubscribe the reports in {textChannel.Mention}.");
            }
            
            var reportChannel = await GetReportChannelAsync(textChannel);

            if (reportChannel?.Scope is null)
            {
                return Respond(description: $"No reports are subscribed in {textChannel.Mention}.");
            }

            var reportScopeSet = ReportScopeSet.FromJson(reportChannel.Scope);

            if (reportScopeSet is null)
            {
                return Respond(description: $"No reports are subscribed in {textChannel.Mention}.");
            }

            if (string.IsNullOrEmpty(Scope))
            {
                reportChannel.Scope = null;
                await _discordBotUnitOfWork.SaveAsync();

                return Respond(description: $"Unsubscribed from all reports in {textChannel.Mention}.", ephemeral: false);
            }

            if (!reportScopeSet.TryRemove(Scope, out string? fullScopeName))
            {
                return Respond(description: $"Cannot unsubscribe from the report scope `{Scope}` in {textChannel.Mention}.");
            }

            var hasAnyScopeAllowed = false;

            foreach (var prop in typeof(ReportScopeSet).GetProperties().Where(x => x.PropertyType.IsSubclassOf(typeof(ReportScope))))
            {
                if (prop.GetValue(reportScopeSet) is ReportScope)
                {
                    hasAnyScopeAllowed = true;
                    break;
                }
            }

            reportChannel.Scope = hasAnyScopeAllowed ? reportScopeSet.ToJson() : null;

            await _discordBotUnitOfWork.SaveAsync();

            return hasAnyScopeAllowed
                ? Respond("Unsubscribed from reports.", $"Unsubscribed from report scope `{fullScopeName}` in {textChannel.Mention}.\nYou can verify your scopes with the '**/report scopes**' command.", ephemeral: false)
                : Respond(description: $"Unsubscribed from all reports in {textChannel.Mention}.", ephemeral: false);
        }

        private async Task<ReportChannelModel?> GetReportChannelAsync(SocketTextChannel textChannel)
        {
            var discordBotGuid = GetDiscordBotGuid();

            if (discordBotGuid is null)
            {
                return null;
            }

            return await _discordBotUnitOfWork.ReportChannels.GetByBotAndTextChannelAsync(discordBotGuid.Value, textChannel);
        }
    }
}
