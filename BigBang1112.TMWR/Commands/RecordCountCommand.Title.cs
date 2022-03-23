using BigBang1112.DiscordBot;
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
        private readonly IWrRepo _repo;
        private readonly IRecordSetService _recordSetService;
        private readonly IMemoryCache _memoryCache;

        //[DiscordBotCommandOption("graph", ApplicationCommandOptionType.Boolean, "Shows the record count \"as the map group progresses\" graph instead.")]
        //public bool Graph { get; set; }

        [DiscordBotCommandOption("title", ApplicationCommandOptionType.String, "Title pack to use.", IsRequired = true)]
        public string TitlePack { get; set; } = default!;

        public async Task<IEnumerable<string>> AutocompleteTitlePackAsync(string value)
        {
            return await _repo.GetTitlePacksAsync(value);
        }

        [DiscordBotCommandOption("groupname", ApplicationCommandOptionType.String, "Map group to use.", IsDefault = true)]
        public string GroupName { get; set; } = default!;

        public async Task<IEnumerable<string>> AutocompleteGroupNameAsync(string value)
        {
            return await _repo.GetMapGroupNamesAsync(value);
        }

        [DiscordBotCommandOption("groupnum", ApplicationCommandOptionType.Integer, "Map group to use.")]
        public string GroupNum { get; set; } = default!;

        public Title(DiscordBotService discordBotService,
                     IWrRepo repo,
                     IRecordSetService recordSetService,
                     IMemoryCache memoryCache) : base(discordBotService)
        {
            _repo = repo;
            _recordSetService = recordSetService;
            _memoryCache = memoryCache;
        }

        public override async Task<DiscordBotMessage> ExecuteAsync(SocketInteraction slashCommand, Deferer deferer)
        {
            var titlePack = await _repo.GetTitlePackAsync(TitlePack);

            if (titlePack is null)
            {
                return new DiscordBotMessage(new EmbedBuilder().WithDescription("No title pack was found.").Build(), ephemeral: true);
            }

            var mapGroupRecordCounts = await _memoryCache.GetOrCreateAsync<Dictionary<MapGroupModel, int>>($"RecordCount_TitlePack_{titlePack.GetTitleUid()}", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);

                await deferer.DeferAsync();

                var recordCounts = new Dictionary<string, Task<RecordSet?>>();

                foreach (var group in titlePack.MapGroups)
                {
                    foreach (var map in group.Maps)
                    {
                        recordCounts.Add(map.MapUid, _recordSetService.GetFromMapAsync("World", map.MapUid));
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

            foreach (var (mapGroup, count) in mapGroupRecordCounts)
            {
                strBuilder.AppendLine($"{mapGroup.DisplayName ?? "TODO ID"}: **{count:N0}**");
            }

            var embed = new EmbedBuilder()
                .WithTitle($"{totalRecordCount:N0} records")
                .WithDescription(strBuilder.ToString())
                .Build();

            return new DiscordBotMessage(embed);
        }
    }
}
