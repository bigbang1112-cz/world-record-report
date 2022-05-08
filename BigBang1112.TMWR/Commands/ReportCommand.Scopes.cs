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

        public Task<IEnumerable<string>> AutocompleteAddAsync(string value)
        {
            return GetScopesLike(value);
        }

        [DiscordBotCommandOption("remove", ApplicationCommandOptionType.String, "Remove a report scope [ManageChannels].")]
        public string? Remove { get; set; }

        public Task<IEnumerable<string>> AutocompleteRemoveAsync(string value)
        {
            return GetScopesLike(value);
        }

        [DiscordBotCommandOption("explain", ApplicationCommandOptionType.String, "Explain a report scope.")]
        public string? Explain { get; set; }

        public Task<IEnumerable<string>> AutocompleteExplainAsync(string value)
        {
            return GetScopesLike(value);
        }

        [DiscordBotCommandOption("other", ApplicationCommandOptionType.Channel, "Specify other channel to apply/see the scopes to/of.")]
        public SocketChannel? OtherChannel { get; set; }

        public Scopes(DiscordBotService discordBotService) : base(discordBotService)
        {

        }

        private static Task<IEnumerable<string>> GetScopesLike(string value)
        {
            var scopes = ReportScopeSet.GetAllPossibleReportScopes()
                            .Where(x => x.ToLower().Contains(value))
                            .Take(25);

            return Task.FromResult(scopes);
        }
    }
}
