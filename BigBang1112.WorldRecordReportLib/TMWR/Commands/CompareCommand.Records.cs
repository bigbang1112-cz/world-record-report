using BigBang1112.WorldRecordReportLib.Enums;
using BigBang1112.WorldRecordReportLib.Services;
using BigBang1112.WorldRecordReportLib.Data;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.TMWR.Models;
using Discord;
using System.Text.RegularExpressions;
using TmEssentials;

using Game = BigBang1112.WorldRecordReportLib.Enums.Game;
using Discord.WebSocket;
using BigBang1112.DiscordBot.Models;

namespace BigBang1112.WorldRecordReportLib.TMWR.Commands;

public partial class CompareCommand
{
    [DiscordBotSubCommand("records", "Compare two records with each other.")]
    public class Records : MapRelatedWithUidCommand
    {
        private readonly IWrUnitOfWork _wrUnitOfWork;
        private readonly RecordStorageService _recordStorageService;

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
                       IWrUnitOfWork wrUnitOfWork,
                       RecordStorageService recordStorageService) : base(tmwrDiscordBotService, wrUnitOfWork)
        {
            _wrUnitOfWork = wrUnitOfWork;
            _recordStorageService = recordStorageService;
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
                var recordSet = await _recordStorageService.GetTM2LeaderboardAsync(map.MapUid);

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

                var loginModels = await _wrUnitOfWork.Logins.GetByNamesAsync(Game.TM2, new[] { rec1.Login, rec2.Login });

                _ = loginModels.TryGetValue(rec1.Login, out LoginModel? rec1LoginModel);
                _ = loginModels.TryGetValue(rec2.Login, out LoginModel? rec2LoginModel);

                record1 = new MiniRecord(rec1.Rank, rec1.Time.TotalMilliseconds, rec1LoginModel?.GetDeformattedNickname().EscapeDiscord() ?? rec1.Login);
                record2 = new MiniRecord(rec2.Rank, rec2.Time.TotalMilliseconds, rec2LoginModel?.GetDeformattedNickname().EscapeDiscord() ?? rec2.Login);
            }
            else if (map.Game.IsTMUF())
            {
                if (map.TmxAuthor is null)
                {
                    return;
                }

                var recordSetTmx = await _recordStorageService.GetTmxLeaderboardAsync((TmxSite)map.TmxAuthor.Site.Id, map.MapUid);

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

                var loginModels = await _wrUnitOfWork.TmxLogins.GetByUserIdsAsync(new[] { rec1.UserId, rec2.UserId }, (TmxSite)map.TmxAuthor.Site.Id);

                _ = loginModels.TryGetValue(rec1.UserId, out var rec1LoginModel);
                _ = loginModels.TryGetValue(rec2.UserId, out var rec2LoginModel);

                record1 = new MiniRecord(rec1.Rank.GetValueOrDefault(), rec1.ReplayTime.TotalMilliseconds, rec1LoginModel?.Nickname ?? rec1.UserId.ToString());
                record2 = new MiniRecord(rec2.Rank.GetValueOrDefault(), rec2.ReplayTime.TotalMilliseconds, rec2LoginModel?.Nickname ?? rec2.UserId.ToString());
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

            var infoUrl = map.GetInfoUrl();

            builder.AddField("Map", infoUrl is null ? mapNameStr : $"[{mapNameStr}]({infoUrl})");
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
