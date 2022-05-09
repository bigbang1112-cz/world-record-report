using BigBang1112.DiscordBot;
using BigBang1112.DiscordBot.Data;
using BigBang1112.WorldRecordReportLib.Models.ReportScopes;
using Discord;

namespace BigBang1112.TMWR.Commands;

public partial class ReportCommand
{
    [DiscordBotSubCommand("subscribe", "Gets the channel information about the subscription, or subscribes to the reports in this channel.")]
    public class Subscribe : DiscordBotCommand
    {
        private readonly IDiscordBotUnitOfWork _discordBotUnitOfWork;

        [DiscordBotCommandOption("scope", ApplicationCommandOptionType.String, "Default scope to use. You can change this with /report scopes", IsRequired = true)]
        public bool? Scope { get; set; }

        public Task<IEnumerable<string>> AutocompleteScopeAsync(string value)
        {
            return Task.FromResult(ReportScopeSet.GetReportScopesLike(value));
        }

        [DiscordBotCommandOption("other", ApplicationCommandOptionType.Channel, "Specify other channel to apply/see the subscription to/of.")]
        public SocketChannel? OtherChannel { get; set; }

        public Subscribe(DiscordBotService discordBotService, IDiscordBotUnitOfWork discordBotUnitOfWork) : base(discordBotService)
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
                throw new Exception("This user is not a guild user");
            }

            if (!guildUser.GuildPermissions.ManageChannels)
            {
                return RespondWithDescriptionEmbed($"You don't have permissions to set the report subscription in <#{textChannel.Id}>.");
            }

            /*await SetReportScopeSetAsync(textChannel, null);

            return Set.Value
                ? RespondWithDescriptionEmbed($"In <#{textChannel.Id}>, things are now reported, **after adding scopes** with `/report wrs scopes add`.")
                : RespondWithDescriptionEmbed($"In <#{textChannel.Id}>, things are **no longer reported**.");*/

            return RespondWithDescriptionEmbed($"In <#{textChannel.Id}>, things are **reported**.");
        }

        private async Task<string?> GetReportScopeSetAsync(SocketTextChannel textChannel)
        {
            var discordBotGuid = GetDiscordBotGuid();

            if (discordBotGuid is null)
            {
                return null;
            }

            var reportSubscription = await _discordBotUnitOfWork.ReportChannels.GetByBotAndTextChannelAsync(discordBotGuid.Value, textChannel);

            return reportSubscription?.Scope;
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
