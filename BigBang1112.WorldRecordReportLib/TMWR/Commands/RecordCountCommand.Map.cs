using BigBang1112.WorldRecordReportLib.Data;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Services;
using Discord;

namespace BigBang1112.WorldRecordReportLib.TMWR.Commands;

public partial class RecordCountCommand
{
    [DiscordBotSubCommand("map", "Shows the amount of records on a map.")]
    public class Map : MapRelatedWithUidCommand
    {
        private readonly RecordStorageService _recordStorageService;

        public Map(TmwrDiscordBotService tmwrDiscordBotService, IWrUnitOfWork wrUnitOfWork, RecordStorageService recordStorageService) : base(tmwrDiscordBotService, wrUnitOfWork)
        {
            _recordStorageService = recordStorageService;
        }

        protected override async Task BuildEmbedResponseAsync(MapModel map, EmbedBuilder builder)
        {
            var recordSet = await _recordStorageService.GetTM2LeaderboardAsync(map.MapUid);

            builder.Title = recordSet is null
                ? "Cannot determine the record count (yet)"
                : $"{recordSet.GetRecordCount():N0} records";

            builder.Description = $"{map.GetHumanizedDeformattedName()} by {map.Author.GetDeformattedNickname().EscapeDiscord()}";

            var infoUrl = map.GetInfoUrl();

            if (infoUrl is not null)
            {
                builder.Description = $"[{builder.Description}]({infoUrl})";
            }
        }
    }
}
