using BigBang1112.Extensions;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;
using Discord;

namespace BigBang1112.TMWR.Commands;

public partial class HistoryCommand
{
    [DiscordBotSubCommand("wr", "Gets the world record history of a map.")]
    public class Wr : MapRelatedWithUidCommand
    {
        private readonly IWrRepo _repo;

        public bool HideTimestamps { get; set; }
        public bool HideNicknames { get; set; }

        public Wr(TmwrDiscordBotService tmwrDiscordBotService, IWrRepo repo) : base(tmwrDiscordBotService, repo)
        {
            _repo = repo;
        }

        protected override async Task BuildEmbedResponseAsync(MapModel map, EmbedBuilder builder)
        {
            var isTMUF = map.Game.IsTMUF();

            var wrs = await _repo.GetWorldRecordHistoryFromMapAsync(map);

            builder.Title = map.GetHumanizedDeformattedName();

            var isStunts = map.IsStuntsMode();

            var desc = string.Join('\n', wrs.Select((x, i) =>
            {
                var time = isStunts ? x.Time.ToString() : x.TimeInt32.ToString(useHundredths: isTMUF);

                var baseStr = $"{wrs.Count - i}) **{time}**";

                if (!HideNicknames && !HideTimestamps)
                {
                    baseStr = $"{baseStr} by {x.GetPlayerNicknameDeformatted()} ({x.DrivenOn.ToTimestampTag(TimestampTagStyles.ShortDate)})";
                }
                else if (HideTimestamps)
                {
                    baseStr = $"{baseStr} by {x.GetPlayerNicknameDeformatted()}";
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
                var historyStartDate = await _repo.GetStartingDateOfHistoryTrackingAsync(map.TitlePack);
                desc += $"\n\nHistory is tracked since {historyStartDate.ToTimestampTag(TimestampTagStyles.ShortDate)}.";
            }

            builder.Description = desc;
            builder.ThumbnailUrl = map.GetThumbnailUrl();
            builder.Url = map.GetTmxUrl();

            builder.Footer = null;
        }
    }
}
