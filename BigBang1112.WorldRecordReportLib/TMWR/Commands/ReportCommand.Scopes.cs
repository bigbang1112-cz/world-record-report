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
    [DiscordBotSubCommand("scopes", "Gets or sets what should be reported.")]
    public class Scopes : TmwrCommand
    {
        private readonly IDiscordBotUnitOfWork _discordBotUnitOfWork;

        [DiscordBotCommandOption("explain", ApplicationCommandOptionType.String, "Explain a report scope.")]
        public string? Explain { get; set; }

        internal static IEnumerable<string> AutocompleteExplain(string value)
        {
            return ReportScopeSet.GetReportScopesLike(value);
        }

        [DiscordBotCommandOption("other", ApplicationCommandOptionType.Channel, "Specify other channel to apply/see the scopes to/of.")]
        public SocketChannel? OtherChannel { get; set; }

        public Scopes(TmwrDiscordBotService tmwrDiscordBotService,
                      IDiscordBotUnitOfWork discordBotUnitOfWork) : base(tmwrDiscordBotService)
        {
            _discordBotUnitOfWork = discordBotUnitOfWork;
        }

        public override async Task<DiscordBotMessage> ExecuteAsync(SocketInteraction slashCommand)
        {
            if (Explain is not null)
            {
                return ExecuteExplain(Explain);
            }

            if (OtherChannel is not SocketTextChannel textChannel)
            {
                if (OtherChannel is not null)
                {
                    return Respond(description: "The specified channel is not a text channel.");
                }

                textChannel = slashCommand.Channel as SocketTextChannel ?? throw new Exception();
            }

            if (slashCommand.User is not SocketGuildUser)
            {
                return Respond(description: "You cannot report to your DMs.");
            }
            
            var reportChannel = await GetReportChannelAsync(textChannel);

            if (reportChannel?.Scope is null)
            {
                return Respond(description: "This channel has no reporting enabled.");
            }

            var reportScopeSet = ReportScopeSet.FromJson(reportChannel.Scope);

            if (reportScopeSet is null)
            {
                return Respond(description: "This channel has no reporting enabled.");
            }

            var scopesFormatted = reportScopeSet.ToJson(enableFormatting: true)
                .Replace("{}", "(all)").Replace("\"Param\"", "values");

            return Respond(title: $"Report scopes in #{textChannel.Name}",
                description: $"```json\n{scopesFormatted}\n```");
        }

        private static DiscordBotMessage ExecuteExplain(string scope)
        {
            var explanation = ReportScopeSet.Explain(scope, out string? exactScope) ?? "No explanation found.";

            return new DiscordBotMessage(new EmbedBuilder { Title = exactScope, Description = explanation }.Build(), ephemeral: true);
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
