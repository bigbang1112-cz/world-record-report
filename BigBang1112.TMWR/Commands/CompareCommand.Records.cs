using BigBang1112.Extensions;
using BigBang1112.TMWR.Models;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;
using BigBang1112.WorldRecordReportLib.Services;
using Discord;
using System.Text.RegularExpressions;
using TmEssentials;

namespace BigBang1112.TMWR.Commands;

public partial class CompareCommand
{
    [DiscordBotSubCommand("records", "Compare two records with each other.")]
    public class Records : MapRelatedWithUidCommand
    {
        private readonly IWrRepo _repo;
        private readonly IRecordSetService _recordSetService;
        private readonly ITmxRecordSetService _tmxRecordSetService;

        [DiscordBotCommandOption("rank1", ApplicationCommandOptionType.Integer,
            "Rank of the record to select for comparison.",
            IsRequired = true,
            MinValue = 1)]
        public long Rank1 { get; set; }

        [DiscordBotCommandOption("rank2", ApplicationCommandOptionType.Integer,
            "Rank of the record to compare against the other record.",
            IsRequired = true,
            MinValue = 1)]
        public long Rank2 { get; set; }

        public Records(TmwrDiscordBotService tmwrDiscordBotService,
                       IWrRepo repo,
                       IRecordSetService recordSetService,
                       ITmxRecordSetService tmxRecordSetService) : base(tmwrDiscordBotService, repo)
        {
            _repo = repo;
            _recordSetService = recordSetService;
            _tmxRecordSetService = tmxRecordSetService;
        }

        protected override async Task BuildEmbedResponseAsync(MapModel map, EmbedBuilder builder)
        {
            builder.Footer.Text = $"({Rank1} vs {Rank2}) {builder.Footer.Text}";

            var rank1Index = (int)Rank1 - 1;
            var rank2Index = (int)Rank2 - 1;

            MiniRecord record1;
            MiniRecord record2;

            if (map.Game.IsTM2())
            {
                var recordSet = await _recordSetService.GetFromMapAsync("World", map.MapUid);

                if (recordSet is null)
                {
                    builder.Title = "Cannot compare records. No records found.";
                    return;
                }

                var rec1 = recordSet.Records.ElementAtOrDefault(rank1Index);

                if (rec1 is null)
                {
                    builder.Title = "Record 1 not found.";
                    return;
                }

                var rec2 = recordSet.Records.ElementAtOrDefault(rank2Index);

                if (rec2 is null)
                {
                    builder.Title = "Record 2 not found.";
                    return;
                }

                var rec1LoginModel = await _repo.GetLoginAsync(rec1.Login);
                var rec2LoginModel = await _repo.GetLoginAsync(rec2.Login);

                record1 = new MiniRecord(rec1.Rank, rec1.Time, rec1LoginModel?.GetDeformattedNickname().EscapeDiscord() ?? rec1.Login);
                record2 = new MiniRecord(rec2.Rank, rec2.Time, rec2LoginModel?.GetDeformattedNickname().EscapeDiscord() ?? rec2.Login);
            }
            else if (map.Game.IsTMUF())
            {
                if (map.TmxAuthor is null)
                {
                    return;
                }

                var recordSetTmx = await _tmxRecordSetService.GetRecordSetAsync(map.TmxAuthor.Site, map);

                if (recordSetTmx is null)
                {
                    return;
                }

                var records = recordSetTmx.Where(x => x.Rank is not null);

                var rec1 = records.ElementAtOrDefault(rank1Index);

                if (rec1 is null)
                {
                    builder.Title = "Record 1 not found.";
                    return;
                }

                var rec2 = records.ElementAtOrDefault(rank2Index);

                if (rec2 is null)
                {
                    builder.Title = "Record 2 not found.";
                    return;
                }

                var rec1LoginModel = await _repo.GetTmxLoginAsync(rec1.UserId);
                var rec2LoginModel = await _repo.GetTmxLoginAsync(rec2.UserId);

                record1 = new MiniRecord(rec1.Rank.GetValueOrDefault(), rec1.ReplayTime, rec1LoginModel?.Nickname ?? rec1.UserId.ToString());
                record2 = new MiniRecord(rec2.Rank.GetValueOrDefault(), rec2.ReplayTime, rec2LoginModel?.Nickname ?? rec2.UserId.ToString());
            }
            else
            {
                throw new Exception();
            }

            builder.Title = "Record comparison";

            var isTMUF = map.Game.IsTMUF();

            builder.Description = $"` {record1.Rank} ` **` {new TimeInt32(record1.TimeOrScore).ToString(useHundredths: isTMUF)} `** by **{record1.Nickname}**\n` {record2.Rank} ` **` {new TimeInt32(record2.TimeOrScore).ToString(useHundredths: isTMUF)} `** by **{record2.Nickname}**";

            var timeDiff = (record1.TimeOrScore - record2.TimeOrScore) / 1000f;
            var timeDiffStr = timeDiff.ToString(isTMUF ? "0.00" : "0.000");

            builder.AddField("Time difference", $"**` {(timeDiff > 0 ? "+" : "")}{timeDiffStr} `**", inline: true);

            var mapNameStr = $"{map.GetHumanizedDeformattedName()} by {map.Author.GetDeformattedNickname().EscapeDiscord()}";

            var tmxUrl = map.GetTmxUrl();

            builder.AddField("Map", tmxUrl is null ? mapNameStr : $"[{mapNameStr}]({map.GetTmxUrl()})");
        }

        public override Task<DiscordBotMessage?> SelectMenuAsync(SocketMessageComponent messageComponent, Deferer deferrer)
        {
            var footerText = messageComponent.Message.Embeds.FirstOrDefault()?.Footer?.Text;

            if (footerText is null)
            {
                return Task.FromResult(default(DiscordBotMessage));
            }

            var rankVsMatch = Regex.Match(footerText, "\\((.*?)\\)");

            if (!rankVsMatch.Success || rankVsMatch.Groups.Count <= 1)
            {
                return Task.FromResult(default(DiscordBotMessage));
            }

            var rankVsStr = rankVsMatch.Groups[1].Value;

            var ranks = rankVsStr.Split(" vs ");

            if (ranks.Length < 2)
            {
                return Task.FromResult(default(DiscordBotMessage));
            }

            if (!long.TryParse(ranks[0], out long rank1))
            {
                return Task.FromResult(default(DiscordBotMessage));
            }

            if (!long.TryParse(ranks[1], out long rank2))
            {
                return Task.FromResult(default(DiscordBotMessage));
            }

            Rank1 = rank1;
            Rank2 = rank2;

            return base.SelectMenuAsync(messageComponent, deferrer);
        }
    }
}
