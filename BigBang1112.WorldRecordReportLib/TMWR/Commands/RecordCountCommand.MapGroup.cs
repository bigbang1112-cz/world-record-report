using BigBang1112.DiscordBot;
using BigBang1112.WorldRecordReportLib.Data;
using Discord;

namespace BigBang1112.WorldRecordReportLib.TMWR.Commands;

public partial class RecordCountCommand
{
    [DiscordBotSubCommand("mapgroup", "Shows the amount of records on each map in map group plus the map group overall.")]
    [UnfinishedDiscordBotCommand]
    public class MapGroup : TmwrCommand
    {
        private readonly IWrUnitOfWork _wrUnitOfWork;

        //[DiscordBotCommandOption("graph", ApplicationCommandOptionType.Boolean, "Shows the record count \"as the map group progresses\" graph instead.")]
        //public bool Graph { get; set; }

        [DiscordBotCommandOption("title", ApplicationCommandOptionType.String, "Title pack to use.")]
        public string TitlePack { get; set; } = default!;

        public async Task<IEnumerable<string>> AutocompleteTitlePackAsync(string value)
        {
            return await _wrUnitOfWork.TitlePacks.GetAllUidsLikeAsync(value);
        }

        [DiscordBotCommandOption("campaign", ApplicationCommandOptionType.String, "Campaign to use.")]
        public string Campaign { get; set; } = default!;

        [DiscordBotCommandOption("groupname", ApplicationCommandOptionType.String, "Map group to use.")]
        public string GroupName { get; set; } = default!;

        public async Task<IEnumerable<string>> AutocompleteGroupNameAsync(string value)
        {
            return await _wrUnitOfWork.MapGroups.GetAllNamesLikeAsync(value);
        }

        [DiscordBotCommandOption("groupnum", ApplicationCommandOptionType.Integer, "Map group to use.")]
        public string GroupNum { get; set; } = default!;

        public MapGroup(TmwrDiscordBotService tmwrDiscordBotService, IWrUnitOfWork wrUnitOfWork) : base(tmwrDiscordBotService)
        {
            _wrUnitOfWork = wrUnitOfWork;
        }
    }
}
