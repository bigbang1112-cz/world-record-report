using BigBang1112.Data;
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

        [DiscordBotCommandOption("forcelinks", ApplicationCommandOptionType.Boolean, "Show the links by force when embed description limit is reached.")]
        public bool ForceLinks { get; set; }

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

            var desc = string.Join('\n', EnumerateHistoryLines(isTMUF, wrs, wrCount, isStunts, links: true));

            if (!ForceLinks && desc.Length > DiscordConsts.EmbedDescriptionLimit)
            {
                desc = string.Join('\n', EnumerateHistoryLines(isTMUF, wrs, wrCount, isStunts, links: ForceLinks));

                desc += "\n\n*Links have not been included due to the embed description limit.*";
            }

            if (desc.Length > DiscordConsts.EmbedDescriptionLimit)
            {
                desc = string.Join('\n', EnumerateHistoryLines(isTMUF, wrs, wrCount, isStunts, links: ForceLinks));
                desc = desc[0..4000] + "...";
                desc += "\n\n*History is too long to fit into embed description.*";
            }

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

        private IEnumerable<string> EnumerateHistoryLines(bool isTMUF, IEnumerable<WorldRecordModel> wrs, int wrCount, bool isStunts, bool links)
        {
            var i = 0;

            var formatter = wrCount switch
            {
                < 10 => "",
                < 100 => "00",
                _ => "000"
            };

            foreach (var wr in wrs)
            {
                var time = isStunts ? wr.Time.ToString() : wr.TimeInt32.ToString(useHundredths: isTMUF);

                var baseStr = $"` {(wrCount - i).ToString(formatter)} ` **` {time} `**";

                var displayName = links
                    ? wr.GetPlayerNicknameMdLink()
                    : wr.GetPlayerNicknameDeformatted().EscapeDiscord();

                if (!HideNicknames && !HideTimestamps)
                {
                    baseStr = $"{baseStr} by **{displayName}** ({wr.DrivenOn.ToTimestampTag(TimestampTagStyles.ShortDate)})";
                }
                else if (HideNicknames && HideTimestamps)
                {

                }
                else if (HideTimestamps)
                {
                    baseStr = $"{baseStr} by **{displayName}**";
                }
                else if (HideNicknames)
                {
                    baseStr = $"{baseStr} ({wr.DrivenOn.ToTimestampTag(TimestampTagStyles.ShortDate)})";
                }

                if (wr.Ignored)
                {
                    yield return $"~~{baseStr}~~";
                }

                yield return baseStr;

                i++;
            }
        }
    }
}
