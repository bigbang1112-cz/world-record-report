using BigBang1112.DiscordBot;
using BigBang1112.DiscordBot.Data;
using BigBang1112.DiscordBot.Models;
using BigBang1112.WorldRecordReportLib.Models.ReportScopes;
using Discord;
using Discord.WebSocket;

namespace BigBang1112.WorldRecordReportLib.TMWR.Commands;

public partial class ReportCommand
{
    [DiscordBotSubCommand("subscribe", "Gets the channel information about the subscription, or subscribes to the reports in this channel.")]
    public class Subscribe : TmwrCommand
    {
        private readonly IDiscordBotUnitOfWork _discordBotUnitOfWork;

        [DiscordBotCommandOption("scope",
            ApplicationCommandOptionType.String,
            "Default scope to use. You can change this anytime with /report scopes.",
            IsRequired = true)]
        public string Scope { get; set; } = "";

        internal static IEnumerable<string> AutocompleteScopeAsync(string value)
        {
            return ReportScopeSet.GetReportScopesLike(value);
        }

        [DiscordBotCommandOption("other", ApplicationCommandOptionType.Channel, "Specify other channel to apply/see the subscription to/of.")]
        public SocketChannel? OtherChannel { get; set; }

        public Subscribe(TmwrDiscordBotService tmwrDiscordBotService, IDiscordBotUnitOfWork discordBotUnitOfWork) : base(tmwrDiscordBotService)
        {
            _discordBotUnitOfWork = discordBotUnitOfWork;
        }

        public override async Task<DiscordBotMessage> ExecuteAsync(SocketInteraction slashCommand)
        {
            if (OtherChannel is not SocketTextChannel textChannel)
            {
                if (OtherChannel is not null)
                {
                    return RespondWithDescriptionEmbed("The specified channel is not a text channel.");
                }

                textChannel = slashCommand.Channel as SocketTextChannel ?? throw new Exception();
            }

            /*if (Set is null)
            {
                return await GetReportScopeSetAsync(textChannel) is null
                    ? RespondWithDescriptionEmbed($"In <#{textChannel.Id}>, things are **not** reported.")
                    : RespondWithDescriptionEmbed($"In <#{textChannel.Id}>, things are **reported**.");
            }*/

            if (slashCommand.User is not SocketGuildUser guildUser)
            {
                return RespondWithDescriptionEmbed("You cannot report to your DMs.");
            }

            if (!guildUser.GuildPermissions.ManageChannels)
            {
                return RespondWithDescriptionEmbed($"You don't have permissions to set the report subscription in <#{textChannel.Id}>.");
            }

            string? fullScopeName;

            var reportScopeSet = await GetReportScopeSetAsync(textChannel);

            if (reportScopeSet is null)
            {
                // Try create a fresh scope set

                if (!ReportScopeSet.TryParse(Scope, out reportScopeSet, out fullScopeName))
                {
                    return RespondWithDescriptionEmbed($"Invalid scope.");
                }
            }
            else if (!reportScopeSet.TryAdd(Scope, out fullScopeName)) // Try update the scope set
            {
                return fullScopeName is null
                    ? RespondWithDescriptionEmbed($"Invalid scope.")
                    : RespondWithDescriptionEmbed($"Scope is already added.");
            }

            var nice = reportScopeSet.TM2;
            var nice2 = fullScopeName;

            /*await SetReportScopeSetAsync(textChannel, null);

            return Set.Value
                ? RespondWithDescriptionEmbed($"In <#{textChannel.Id}>, things are now reported, **after adding scopes** with `/report wrs scopes add`.")
                : RespondWithDescriptionEmbed($"In <#{textChannel.Id}>, things are **no longer reported**.");*/

            return RespondWithDescriptionEmbed($"In <#{textChannel.Id}>, reports from scope XXX are **reported**.");
        }

        private async Task<ReportScopeSet?> GetReportScopeSetAsync(SocketTextChannel textChannel)
        {
            var discordBotGuid = GetDiscordBotGuid();

            if (discordBotGuid is null)
            {
                return null;
            }

            var reportSubscription = await _discordBotUnitOfWork.ReportChannels.GetByBotAndTextChannelAsync(discordBotGuid.Value, textChannel);

            if (reportSubscription?.Scope is null)
            {
                return null;
            }
            
            return ReportScopeSet.FromJson(reportSubscription.Scope);
        }

        private async Task SetReportScopeSetAsync(SocketTextChannel textChannel, string scopeSet)
        {
            var discordBotGuid = GetDiscordBotGuid();

            if (discordBotGuid is null)
            {
                throw new Exception("Missing discord bot guid");
            }

            await _discordBotUnitOfWork.ReportChannels.AddOrUpdateAsync(discordBotGuid.Value, textChannel, scopeSet);

            await _discordBotUnitOfWork.SaveAsync();
        }

        private static DiscordBotMessage RespondWithDescriptionEmbed(string description)
        {
            var embed = new EmbedBuilder()
                .WithDescription(description)
                .Build();

            return new DiscordBotMessage(embed, ephemeral: true);
        }
    }
}
