using BigBang1112.DiscordBot;
using BigBang1112.Extensions;
using BigBang1112.TMWR.Extensions;
using BigBang1112.WorldRecordReportLib.Data;
using BigBang1112.WorldRecordReportLib.Models;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;
using BigBang1112.WorldRecordReportLib.Services;
using Discord;
using Microsoft.Extensions.Caching.Memory;
using System.Text;

namespace BigBang1112.TMWR.Commands;

public partial class RecordCountCommand
{
    [DiscordBotSubCommand("title", "Shows the amount of records in the title pack overall.")]
    public class Title : DiscordBotCommand
    {
        private readonly IWrUnitOfWork _wrUnitOfWork;
        private readonly RecordStorageService _recordStorageService;
        private readonly IMemoryCache _memoryCache;

        //[DiscordBotCommandOption("graph", ApplicationCommandOptionType.Boolean, "Shows the record count \"as the map group progresses\" graph instead.")]
        //public bool Graph { get; set; }

        [DiscordBotCommandOption("title", ApplicationCommandOptionType.String, "Title pack to use.", IsRequired = true)]
        public string TitlePack { get; set; } = default!;

        public async Task<IEnumerable<string>> AutocompleteTitlePackAsync(string value)
        {
            return await _wrUnitOfWork.TitlePacks.GetAllUidsLikeAsync(value);
        }

        public Title(DiscordBotService discordBotService,
                     IWrUnitOfWork wrUnitOfWork,
                     RecordStorageService recordStorageService,
                     IMemoryCache memoryCache) : base(discordBotService)
        {
            _wrUnitOfWork = wrUnitOfWork;
            _recordStorageService = recordStorageService;
            _memoryCache = memoryCache;
        }

        public override async Task<DiscordBotMessage> ExecuteAsync(SocketInteraction slashCommand, Deferer deferer)
        {
            var titlePack = await _wrUnitOfWork.TitlePacks.GetByFullUidAsync(TitlePack);

            if (titlePack is null)
            {
                return new DiscordBotMessage(new EmbedBuilder().WithDescription("No title pack was found. Make sure you've entered the whole title ID, as shortcuts aren't supported at the moment.").Build(), ephemeral: true);
            }

            var mapGroupRecordCounts = await _memoryCache.GetOrCreateAsync($"RecordCount_TitlePack_{titlePack.GetTitleUid()}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);

                await deferer.DeferAsync();

                var recordCounts = new Dictionary<string, Task<LeaderboardTM2?>>();

                foreach (var group in titlePack.MapGroups)
                {
                    foreach (var map in group.Maps)
                    {
                        recordCounts.Add(map.MapUid, _recordStorageService.GetTM2LeaderboardAsync(map.MapUid));
                    }
                }

                _ = await Task.WhenAll(recordCounts.Values);

                return titlePack.MapGroups
                    .ToDictionary(x => x, x => x.Maps
                        .Select(x => recordCounts[x.MapUid])
                        .Sum(x => x.Result?.GetRecordCount() ?? 0));
            });

            var totalRecordCount = mapGroupRecordCounts.Sum(x => x.Value);

            var strBuilder = new StringBuilder();

            foreach (var (mapGroup, count) in mapGroupRecordCounts.OrderBy(x => x.Key.Number))
            {
                strBuilder.AppendLine($"{mapGroup.DisplayName ?? "TODO ID"}: **{count:N0}**");
            }

            var embed = new EmbedBuilder()
                .WithTitle($"{totalRecordCount:N0} records")
                .WithDescription(strBuilder.ToString())
                .WithBotFooter(TitlePack)
                .Build();

            return new DiscordBotMessage(embed);
        }
    }
}
