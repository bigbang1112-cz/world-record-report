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
    [DiscordBotSubCommand("wrs")]
    public class Wrs : MapRelatedWithUidCommand
    {
        private readonly IWrRepo _repo;
        private readonly IRecordSetService _recordSetService;
        private readonly ITmxRecordSetService _tmxRecordSetService;

        [DiscordBotCommandOption("guid1", ApplicationCommandOptionType.String,
            "GUID of the world record to select for comparison.",
            IsRequired = true)]
        public string Guid1 { get; set; } = default!;

        public async Task<IEnumerable<string>> AutocompleteGuid1Async(string value)
        {
            return await _repo.GetWorldRecordGuidsAsync(value);
        }

        [DiscordBotCommandOption("guid2", ApplicationCommandOptionType.String,
            "GUID of the world record to compare against the other record.",
            IsRequired = true)]
        public string Guid2 { get; set; } = default!;

        public async Task<IEnumerable<string>> AutocompleteGuid2Async(string value)
        {
            return await _repo.GetWorldRecordGuidsAsync(value);
        }

        public Wrs(TmwrDiscordBotService tmwrDiscordBotService,
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
            builder.Footer.Text = $"{Guid1}\n{Guid2}\n{builder.Footer.Text}";

            var guid1 = new Guid(Guid1);
            var guid2 = new Guid(Guid2);

            var wr1 = await _repo.GetWorldRecordAsync(guid1);

            if (wr1 is null)
            {
                builder.Title = "World record 1 not found.";
                return;
            }

            var wr2 = await _repo.GetWorldRecordAsync(guid2);

            if (wr2 is null)
            {
                builder.Title = "World record 2 not found.";
                return;
            }

            if (wr1.Map.MapUid != wr2.Map.MapUid)
            {
                builder.Title = "Maps of the world records don't match.";
                return;
            }

            builder.Title = "Record comparison";

            var isTMUF = map.Game.IsTMUF();

            builder.Description = $"**` {wr1.TimeInt32.ToString(useHundredths: isTMUF)} `** by **{wr1.GetPlayerNicknameDeformatted().EscapeDiscord()}**\n**` {wr2.TimeInt32.ToString(useHundredths: isTMUF)} `** by **{wr2.GetPlayerNicknameDeformatted().EscapeDiscord()}**";

            var timeDiffStr = ((wr1.Time - wr2.Time) / 1000f).ToString(isTMUF ? "0.00" : "0.000");

            builder.AddField("Time difference", $"**` {timeDiffStr} `**", inline: true);

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

            var ranks = footerText.Split('\n');

            if (ranks.Length < 2)
            {
                return Task.FromResult(default(DiscordBotMessage));
            }

            Guid1 = ranks[0];
            Guid2 = ranks[1];

            return base.SelectMenuAsync(messageComponent, deferrer);
        }
    }
}
