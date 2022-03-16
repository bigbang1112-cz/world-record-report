using BigBang1112.Extensions;
using BigBang1112.WorldRecordReportLib.Models;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;
using BigBang1112.WorldRecordReportLib.Services;
using Discord;
using TmEssentials;

namespace BigBang1112.TMWR.Commands;

public partial class MapCommand
{
    [DiscordBotSubCommand("info", "Gets information about the map.")]
    public class Info : MapRelatedWithUidCommand
    {
        private readonly IWrRepo _repo;
        private readonly IRecordSetService _recordSetService;

        public Info(TmwrDiscordBotService tmwrDiscordBotService, IWrRepo repo, IRecordSetService recordSetService) : base(tmwrDiscordBotService, repo)
        {
            _repo = repo;
            _recordSetService = recordSetService;
        }

        protected override Task<ComponentBuilder?> CreateComponentsAsync(MapModel map, bool isModified)
        {
            var builder = new ComponentBuilder()
                .WithButton("Top 10", CreateCustomId("top10"), ButtonStyle.Secondary, disabled: true)
                .WithButton("WR", CreateCustomId("wrdetails"), ButtonStyle.Secondary, disabled: true)
                .WithButton("WR history", CreateCustomId("wrhistory"), ButtonStyle.Secondary, disabled: true);

            return Task.FromResult(builder)!;
        }

        protected override async Task BuildEmbedResponseAsync(MapModel map, EmbedBuilder builder)
        {
            builder.Title = $"{map.GetHumanizedDeformattedName()} by {map.Author.GetDeformattedNickname()}";
            builder.ThumbnailUrl = map.GetThumbnailUrl();
            builder.Url = map.GetTmxUrl();


            if (map.TitlePack is null)
            {
                builder.AddField("Game", map.Game, inline: true);
            }
            else
            { 
                builder.AddField("Game / Title pack", map.TitlePack, inline: true);
            }

            builder.AddField("Environment", map.Environment, inline: true);

            var wr = await _repo.GetWorldRecordAsync(map);

            if (wr is not null)
            {
                builder.AddField("World record",
                    $"{wr.GetTimeFormattedToGame()} by {wr.GetPlayerNicknameDeformatted()}");

                builder.AddField("World record driven on", wr.DrivenOn.ToTimestampTag(TimestampTagStyles.LongDateTime));
            }

            if (map.LastActivityOn.HasValue)
            {
                builder.AddField("Last TMX activity", map.LastActivityOn.Value.ToTimestampTag(TimestampTagStyles.Relative), inline: true);
            }

            var lastChange = await _repo.GetLastRecordSetChangeOnMapAsync(map);

            if (lastChange is not null)
            {
                builder.AddField("Last activity", lastChange.DrivenBefore.ToTimestampTag(TimestampTagStyles.Relative), inline: true);
            }

            var lastTop10Change = await _repo.GetLastRecordSetDetailedChangeOnMapAsync(map);

            var recordSet = default(RecordSet);

            if (lastTop10Change is not null)
            {
                if (lastTop10Change.DrivenBefore.HasValue)
                {
                    var oldestChange = (await _repo.GetOldestRecordSetDetailedChangeOnMapAsync(map))?.DrivenBefore;

                    var drivenBefore = lastTop10Change.DrivenBefore.Value;

                    var timestampTag = drivenBefore.ToTimestampTag(TimestampTagStyles.Relative).ToString();

                    if (oldestChange is not null && drivenBefore - oldestChange.Value < TimeSpan.FromDays(1))
                    {
                        timestampTag += "+";
                    }

                    builder.AddField("Last Top 10 activity", timestampTag, inline: true);
                }

                var typeOfActivity = lastTop10Change.Type switch
                {
                    RecordSetDetailedChangeType.New => "New record",
                    RecordSetDetailedChangeType.Improvement => "Improved record",
                    RecordSetDetailedChangeType.Removed => "Removed record",
                    RecordSetDetailedChangeType.Worsen => "Worsen record",
                    RecordSetDetailedChangeType.PushedOff => "Pushed off record",
                    _ => "Unknown activity"
                };

                var time = default(TimeInt32?);

                if (lastTop10Change.Type == RecordSetDetailedChangeType.New)
                {
                    recordSet = await _recordSetService.GetFromMapAsync("World", map.MapUid);

                    if (recordSet is not null)
                    {
                        var record = recordSet.Records.FirstOrDefault(x => x.Login == lastTop10Change.Login.Name);

                        if (record is not null)
                        {
                            time = new TimeInt32(record.Time);
                        }
                    }
                }
                else
                {
                    time = new TimeInt32(lastTop10Change.Time.GetValueOrDefault());
                }

                builder.AddField($"Last Top 10 activity - {typeOfActivity}", $"{time.ToTmString(useHundredths: map.Game.IsTMUF())} by {lastTop10Change.Login.GetDeformattedNickname()}");
            }

            if (recordSet is null)
            {
                recordSet = await _recordSetService.GetFromMapAsync("World", map.MapUid);
            }

            if (recordSet is not null)
            {
                builder.AddField("Record count", recordSet.GetRecordCount().ToString("N0"));
            }
        }
    }
}
