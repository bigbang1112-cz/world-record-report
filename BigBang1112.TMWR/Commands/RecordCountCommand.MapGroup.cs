using BigBang1112.DiscordBot;
using BigBang1112.WorldRecordReportLib.Repos;
using Discord;

namespace BigBang1112.TMWR.Commands;

public partial class RecordCountCommand
{
    [DiscordBotSubCommand("mapgroup", "Shows the amount of records on each map in map groups plus the map group overall.")]
    public class MapGroup : DiscordBotCommand
    {
        private readonly IWrRepo _repo;

        [DiscordBotCommandOption("graph", ApplicationCommandOptionType.Boolean, "Shows the record count \"as the map group progresses\" graph instead.")]
        public bool Graph { get; set; }

        [DiscordBotCommandOption("campaign", ApplicationCommandOptionType.String, "Campaign to use.", IsRequired = true)]
        public string Campaign { get; set; } = default!;

        [DiscordBotCommandOption("groupname", ApplicationCommandOptionType.String, "Map group to use.", IsDefault = true)]
        public string GroupName { get; set; } = default!;

        public async Task<IEnumerable<string>> AutocompleteGroupNameAsync(string value)
        {
            return await _repo.GetMapGroupNamesAsync(value);
        }

        [DiscordBotCommandOption("groupnum", ApplicationCommandOptionType.Integer, "Map group to use.")]
        public string GroupNum { get; set; } = default!;

        public MapGroup(DiscordBotService discordBotService, IWrRepo repo) : base(discordBotService)
        {
            _repo = repo;
        }
    }
}
