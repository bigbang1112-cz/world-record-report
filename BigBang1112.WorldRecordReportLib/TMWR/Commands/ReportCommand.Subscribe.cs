using BigBang1112.DiscordBot;
using BigBang1112.DiscordBot.Data;
using BigBang1112.DiscordBot.Models;
using BigBang1112.DiscordBot.Models.Db;
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
        private readonly DiscordBotDataService _discordBotDataService;

        [DiscordBotCommandOption("scope",
            ApplicationCommandOptionType.String,
            "Default scope to use. You can change this anytime with (un)subscribe or /report scopes.",
            IsRequired = true)]
        public string Scope { get; set; } = "";

        internal static IEnumerable<string> AutocompleteScopeAsync(string value)
        {
            return ReportScopeSet.GetReportScopesLike(value);
        }

        [DiscordBotCommandOption("other", ApplicationCommandOptionType.Channel, "Specify other channel to apply/see the subscription to/of.")]
        public SocketChannel? OtherChannel { get; set; }

        public Subscribe(TmwrDiscordBotService tmwrDiscordBotService,
                         IDiscordBotUnitOfWork discordBotUnitOfWork,
                         DiscordBotDataService discordBotDataService) : base(tmwrDiscordBotService)
        {
            _discordBotUnitOfWork = discordBotUnitOfWork;
            _discordBotDataService = discordBotDataService;
        }

        public override async Task<DiscordBotMessage> ExecuteAsync(SocketInteraction slashCommand)
        {
            if (OtherChannel is not SocketTextChannel textChannel)
            {
                if (OtherChannel is not null)
                {
                    return Respond(description: "The specified channel is not a text channel.");
                }

                textChannel = slashCommand.Channel as SocketTextChannel ?? throw new Exception();
            }

            if (slashCommand.User is not SocketGuildUser guildUser)
            {
                return Respond(description: "You cannot report to your DMs.");
            }

            if (!guildUser.GuildPermissions.ManageChannels)
            {
                return Respond(description: $"You don't have permissions to set the report subscription in {textChannel.Mention}.");
            }

            // do not allow adding more subscriptions if reaching 5 report channels already on a guild
            var count = await _discordBotUnitOfWork.ReportChannels.CountByJoinedGuildAsync(textChannel.Guild.Id);

            if (count >= LimitConsts.MaxReportChannelsPerGuild)
            {
                return Respond(description: $"You cannot add more than {LimitConsts.MaxReportChannelsPerGuild} report channels to a guild.");
            }

            var reportChannel = await GetReportChannelAsync(textChannel);

            var reportScopeSet = reportChannel?.Scope is null ? null
                : ReportScopeSet.FromJson(reportChannel.Scope);

            string? fullScopeName;

            if (reportScopeSet is null)
            {
                // Try create a fresh scope set

                if (!ReportScopeSet.TryParse(Scope, out reportScopeSet, out fullScopeName, out string? specificValueIssue))
                {
                    return specificValueIssue is null
                        ? Respond("Invalid scope")
                        : Respond("Invalid scope", $"Sub-scope `{specificValueIssue}` is not valid.");
                }
            }
            else if (!reportScopeSet.TryAdd(Scope, out fullScopeName)) // Try update the scope set
            {
                return fullScopeName is null
                    ? Respond("Invalid scope.")
                    : Respond("Scope is already added.");
            }

            if (reportChannel is null)
            {
                await _discordBotUnitOfWork.ReportChannels.AddAsync(new ReportChannelModel
                {
                    Channel = await _discordBotUnitOfWork.DiscordBotChannels.GetOrAddAsync(textChannel),
                    JoinedGuild = await _discordBotUnitOfWork.DiscordBotJoinedGuilds.GetOrAddAsync(GetDiscordBotGuid() ?? throw new Exception(), textChannel.Guild),
                    Scope = reportScopeSet.ToJson()
                });
            }
            else
            {
                reportChannel.Scope = reportScopeSet.ToJson();
            }

            await _discordBotUnitOfWork.SaveAsync();

            return Respond(title: "Subscribed to reports!",
                description: $"Subscribed to `{fullScopeName}` reports in {textChannel.Mention}.\nYou can verify your scopes with the '**/report scopes**' command.", ephemeral: false);
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
