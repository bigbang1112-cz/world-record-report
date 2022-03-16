using BigBang1112.DiscordBot;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;
using BigBang1112.WorldRecordReportLib.Services;
using Discord;

namespace BigBang1112.TMWR.Commands;

[DiscordBotCommand("recordcount")]
public class RecordCountCommand : DiscordBotCommand
{
    public RecordCountCommand(DiscordBotService discordBotService) : base(discordBotService)
    {
        
    }

    public override Task<DiscordBotMessage> ExecuteAsync(SocketSlashCommand slashCommand)
    {
        throw new NotImplementedException();
    }

    [DiscordBotSubCommand("map", "Shows the amount of records on a map.")]
    public class Map : MapRelatedWithUidCommand
    {
        private readonly IRecordSetService _recordSetService;

        [DiscordBotCommandOption("historygraph", ApplicationCommandOptionType.Boolean, "Shows the record count \"over time\" graph instead.")]
        public bool HistoryGraph { get; set; }

        public Map(TmwrDiscordBotService tmwrDiscordBotService, IWrRepo repo, IRecordSetService recordSetService) : base(tmwrDiscordBotService, repo)
        {
            _recordSetService = recordSetService;
        }

        protected override async Task BuildEmbedResponseAsync(MapModel map, EmbedBuilder builder)
        {
            var recordSet = await _recordSetService.GetFromMapAsync("World", map.MapUid);

            if (recordSet is null)
            {
                builder.Title = "Cannot determine the record count (yet)";
            }
            else
            {
                builder.Title = $"{recordSet.GetRecordCount():N0} records";
            }

            builder.Description = $"{map.GetHumanizedDeformattedName()} by {map.Author.GetDeformattedNickname()}";
        }
    }

    [DiscordBotSubCommand("mapgroup", "Shows the amount of records on each map in map groups plus the map group overall.")]
    public class MapGroup : DiscordBotCommand
    {
        [DiscordBotCommandOption("historygraph", ApplicationCommandOptionType.Boolean, "Shows the record count \"over time\" graph instead.")]
        public bool HistoryGraph { get; set; }

        [DiscordBotCommandOption("graph", ApplicationCommandOptionType.Boolean, "Shows the record count \"as the map group progresses\" graph instead.")]
        public bool Graph { get; set; }

        public MapGroup(DiscordBotService discordBotService) : base(discordBotService)
        {

        }

        public override Task<DiscordBotMessage> ExecuteAsync(SocketSlashCommand slashCommand)
        {
            throw new NotImplementedException();
        }
    }

    [DiscordBotSubCommand("campaign", "Shows the amount of records on each map in a campaign plus the campaign overall.")]
    public class Campaign : DiscordBotCommand
    {
        [DiscordBotCommandOption("historygraph", ApplicationCommandOptionType.Boolean, "Shows the record count \"over time\" graph instead.")]
        public bool HistoryGraph { get; set; }

        [DiscordBotCommandOption("graph", ApplicationCommandOptionType.Boolean, "Shows the record count \"as the campaign progresses\" graph instead.")]
        public bool Graph { get; set; }

        public Campaign(DiscordBotService discordBotService) : base(discordBotService)
        {

        }

        public override Task<DiscordBotMessage> ExecuteAsync(SocketSlashCommand slashCommand)
        {
            throw new NotImplementedException();
        }
    }
}
