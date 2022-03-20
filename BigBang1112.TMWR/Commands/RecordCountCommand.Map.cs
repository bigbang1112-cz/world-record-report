using BigBang1112.Extensions;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;
using BigBang1112.WorldRecordReportLib.Services;
using Discord;

namespace BigBang1112.TMWR.Commands;

public partial class RecordCountCommand
{
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
            if (HistoryGraph)
            {
                return;
            }

            var recordSet = await _recordSetService.GetFromMapAsync("World", map.MapUid);

            if (recordSet is null)
            {
                builder.Title = "Cannot determine the record count (yet)";
            }
            else
            {
                builder.Title = $"{recordSet.GetRecordCount():N0} records";
            }

            builder.Description = $"{map.GetHumanizedDeformattedName()} by {map.Author.GetDeformattedNickname().EscapeDiscord()}";

            var tmxUrl = map.GetTmxUrl();

            if (tmxUrl is not null)
            {
                builder.Description = $"[{builder.Description}]({tmxUrl})";
            }
        }
    }
}
