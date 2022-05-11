using BigBang1112.DiscordBot;
using BigBang1112.WorldRecordReportLib.Models.ReportScopes;
using Discord;

namespace BigBang1112.TMWR.Commands;

public partial class ReportCommand
{
    [DiscordBotSubCommand("scopes", "Gets or sets what should be reported.")]
    public class Scopes : DiscordBotCommand
    {
        [DiscordBotCommandOption("add", ApplicationCommandOptionType.String, "Add a report scope [ManageChannels].")]
        public string? Add { get; set; }

        internal static IEnumerable<string> AutocompleteAdd(string value)
        {
            return ReportScopeSet.GetReportScopesLike(value);
        }

        [DiscordBotCommandOption("remove", ApplicationCommandOptionType.String, "Remove a report scope [ManageChannels].")]
        public string? Remove { get; set; }

        internal static IEnumerable<string> AutocompleteRemove(string value)
        {
            return ReportScopeSet.GetReportScopesLike(value);
        }

        [DiscordBotCommandOption("explain", ApplicationCommandOptionType.String, "Explain a report scope.")]
        public string? Explain { get; set; }

        internal static IEnumerable<string> AutocompleteExplain(string value)
        {
            return ReportScopeSet.GetReportScopesLike(value);
        }

        [DiscordBotCommandOption("other", ApplicationCommandOptionType.Channel, "Specify other channel to apply/see the scopes to/of.")]
        public SocketChannel? OtherChannel { get; set; }

        public Scopes(DiscordBotService discordBotService) : base(discordBotService)
        {

        }
    }
}
