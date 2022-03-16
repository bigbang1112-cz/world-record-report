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

            var desc = string.Join('\n', wrs.Select((x, i) =>
            {
                if (HideNicknames && HideTimestamps)
                {
                    return $"{wrs.Count - i}. **{x.TimeInt32.ToString(useHundredths: isTMUF)}**";
                }

                if (HideTimestamps)
                {
                    return $"{wrs.Count - i}. **{x.TimeInt32.ToString(useHundredths: isTMUF)}** by {x.GetPlayerNicknameDeformatted()}";
                }

                if (HideNicknames)
                {
                    return $"{wrs.Count - i}. **{x.TimeInt32.ToString(useHundredths: isTMUF)}** ({x.DrivenOn.ToTimestampTag()})";
                }

                return $"{wrs.Count - i}. **{x.TimeInt32.ToString(useHundredths: isTMUF)}** by {x.GetPlayerNicknameDeformatted()} ({x.DrivenOn.ToTimestampTag(TimestampTagStyles.ShortDate)})";
            }));

            builder.Description = desc;

            var thumbnailUrl = map.GetThumbnailUrl();

            if (thumbnailUrl is not null)
            {
                builder.ThumbnailUrl = thumbnailUrl;
            }

            builder.Footer = null;
        }
    }
}
