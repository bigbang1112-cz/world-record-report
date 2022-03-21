using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;
using BigBang1112.WorldRecordReportLib.Services;
using Discord;
using TmEssentials;

namespace BigBang1112.TMWR.Commands;

public partial class CompareCommand
{
    [DiscordBotSubCommand("records")]
    public class Records : MapRelatedWithUidCommand
    {
        private readonly IRecordSetService _recordSetService;

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

        public Records(TmwrDiscordBotService tmwrDiscordBotService, IWrRepo repo, IRecordSetService recordSetService) : base(tmwrDiscordBotService, repo)
        {
            _recordSetService = recordSetService;
        }

        protected override async Task BuildEmbedResponseAsync(MapModel map, EmbedBuilder builder)
        {
            var recordSet = await _recordSetService.GetFromMapAsync("World", map.MapUid);

            if (recordSet is null)
            {
                builder.Title = "Cannot compare records. No records found.";
                return;
            }

            var record1 = recordSet.Records.ElementAtOrDefault((int)Rank1 - 1);

            if (record1 is null)
            {
                builder.Title = "Record 1 not found.";
                return;
            }

            var record2 = recordSet.Records.ElementAtOrDefault((int)Rank2 - 1);

            if (record2 is null)
            {
                builder.Title = "Record 2 not found.";
                return;
            }

            var isTMUF = map.Game.IsTMUF();

            builder.AddField("Record 1", $"{record1.Rank}) {new TimeInt32(record1.Time).ToString(useHundredths: isTMUF)} by {record1.Login}");
            builder.AddField("Record 2", $"{record2.Rank}) {new TimeInt32(record2.Time).ToString(useHundredths: isTMUF)} by {record2.Login}", inline: true);
        }
    }
}
