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

        public Map(TmwrDiscordBotService tmwrDiscordBotService, IWrRepo repo, IRecordSetService recordSetService) : base(tmwrDiscordBotService, repo)
        {
            _recordSetService = recordSetService;
        }

        protected override async Task BuildEmbedResponseAsync(MapModel map, EmbedBuilder builder)
        {
            var recordSet = await _recordSetService.GetFromMapAsync("World", map.MapUid);

            builder.Title = recordSet is null
                ? "Cannot determine the record count (yet)"
                : $"{recordSet.GetRecordCount():N0} records";

            builder.Description = $"{map.GetHumanizedDeformattedName()} by {map.Author.GetDeformattedNickname().EscapeDiscord()}";

            var tmxUrl = map.GetTmxUrl();

            if (tmxUrl is not null)
            {
                builder.Description = $"[{builder.Description}]({tmxUrl})";
            }
        }
    }
}
