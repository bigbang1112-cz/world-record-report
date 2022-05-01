using BigBang1112.Extensions;
using BigBang1112.WorldRecordReportLib.Data;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;
using Discord;

namespace BigBang1112.TMWR.Commands;

public partial class HistoryCommand
{
    [DiscordBotSubCommand("wr", "Gets the world record history of a map.")]
    public class Wr : MapRelatedWithUidCommand
    {
        private readonly IWrUnitOfWork _wrUnitOfWork;

        public bool HideTimestamps { get; set; }
        public bool HideNicknames { get; set; }

        public Wr(TmwrDiscordBotService tmwrDiscordBotService, IWrUnitOfWork wrUnitOfWork) : base(tmwrDiscordBotService, wrUnitOfWork)
        {
            _wrUnitOfWork = wrUnitOfWork;
        }

        protected override async Task BuildEmbedResponseAsync(MapModel map, EmbedBuilder builder)
        {
            var isTMUF = map.Game.IsTMUF();

            var wrs = await _wrUnitOfWork.WorldRecords.GetHistoryByMapAsync(map);

            builder.Title = map.GetHumanizedDeformattedName();
            builder.ThumbnailUrl = map.GetThumbnailUrl();
            builder.Url = map.GetInfoUrl();
            builder.Footer = null;

            if (!wrs.Any())
            {
                builder.Description = "No world records were tracked.";
                return;
            }

            var wrCount = wrs.Count();

            var isStunts = map.IsStuntsMode();

            var desc = string.Join('\n', wrs.Select((x, i) =>
            {
                var time = isStunts ? x.Time.ToString() : x.TimeInt32.ToString(useHundredths: isTMUF);

                var baseStr = $"` {wrCount - i} ` **` {time} `**";

                if (!HideNicknames && !HideTimestamps)
                {
                    baseStr = $"{baseStr} by **{x.GetPlayerNicknameDeformatted().EscapeDiscord()}** ({x.DrivenOn.ToTimestampTag(TimestampTagStyles.ShortDate)})";
                }
                else if (HideNicknames && HideTimestamps)
                {

                }
                else if (HideTimestamps)
                {
                    baseStr = $"{baseStr} by **{x.GetPlayerNicknameDeformatted().EscapeDiscord()}**";
                }
                else if (HideNicknames)
                {
                    baseStr = $"{baseStr} ({x.DrivenOn.ToTimestampTag(TimestampTagStyles.ShortDate)})";
                }

                if (x.Ignored)
                {
                    return $"~~{baseStr}~~";
                }

                return baseStr;
            }));

            if (map.TitlePack is not null)
            {
                var historyStartDate = await _wrUnitOfWork.WorldRecords.GetStartingDateOfHistoryTrackingByTitlePackAsync(map.TitlePack);

                if (historyStartDate.HasValue)
                {
                    desc += $"\n\nHistory is tracked since {historyStartDate.Value.ToTimestampTag(TimestampTagStyles.ShortDate)}.";
                }
            }

            builder.Description = desc;
        }
    }
}
