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
        private readonly TmwrDiscordBotService _tmwrDiscordBotService;
        private readonly IWrRepo _repo;
        private readonly IRecordSetService _recordSetService;

        public Info(TmwrDiscordBotService tmwrDiscordBotService, IWrRepo repo, IRecordSetService recordSetService) : base(tmwrDiscordBotService, repo)
        {
            _tmwrDiscordBotService = tmwrDiscordBotService;
            _repo = repo;
            _recordSetService = recordSetService;
        }

        protected override Task<ComponentBuilder?> CreateComponentsAsync(MapModel map, bool isModified)
        {
            var builder = new ComponentBuilder()
                .WithButton("Top 10", CreateCustomId($"top10-{map.MapUid}"), ButtonStyle.Secondary, disabled: false)
                .WithButton("World record", CreateCustomId($"wrdetails-{map.MapUid}"), ButtonStyle.Secondary, disabled: false)
                .WithButton("World record history", CreateCustomId($"wrhistory-{map.MapUid}"), ButtonStyle.Secondary, disabled: false)
                .WithButton("Record count history", CreateCustomId($"counthistory-{map.MapUid}"), ButtonStyle.Secondary, disabled: false);

            return Task.FromResult(builder)!;
        }

        protected override async Task BuildEmbedResponseAsync(MapModel map, EmbedBuilder builder)
        {
            builder.Title = $"{map.GetHumanizedDeformattedName()} by {map.Author.GetDeformattedNickname()}";
            builder.ThumbnailUrl = map.GetThumbnailUrl();
            builder.Url = map.GetTmxUrl();

            if (map.TitlePack is null)
            {
                builder.AddField("Game", map.IntendedGame ?? map.Game, inline: true);
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
                recordSet = await AddLastTop10ActivityAsync(map, builder, lastTop10Change);
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

        private async Task<RecordSet?> AddLastTop10ActivityAsync(MapModel map,
                                                                 EmbedBuilder builder,
                                                                 RecordSetDetailedChangeModel lastTop10Change)
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

            var recordSet = await _recordSetService.GetFromMapAsync("World", map.MapUid);

            var activityText = "No details available";

            if (lastTop10Change.Type == RecordSetDetailedChangeType.New)
            {
                if (recordSet is not null)
                {
                    var record = recordSet.Records.FirstOrDefault(x => x.Login == lastTop10Change.Login.Name);

                    if (record is not null)
                    {
                        var time = new TimeInt32(record.Time);
                        var rank = record.Rank;

                        activityText = $"{rank}) {time.ToString(useHundredths: map.Game.IsTMUF())} by {lastTop10Change.Login.GetDeformattedNickname()}";
                    }
                }
            }
            else if (lastTop10Change.Type == RecordSetDetailedChangeType.Improvement)
            {
                var prevTime = new TimeInt32(lastTop10Change.Time.GetValueOrDefault());
                var prevRank = lastTop10Change.Rank.GetValueOrDefault().ToString();

                if (recordSet is null)
                {
                    activityText = $"{prevTime.ToString(useHundredths: map.Game.IsTMUF())} (rank: {prevRank}) to [unknown] by {lastTop10Change.Login.GetDeformattedNickname()}";
                }
                else
                {
                    var record = recordSet.Records.FirstOrDefault(x => x.Login == lastTop10Change.Login.Name);

                    if (record is not null)
                    {
                        var time = new TimeInt32(record.Time);
                        var rank = record.Rank;

                        activityText = $"**From:** {prevTime.ToString(useHundredths: map.Game.IsTMUF())} (rank: {prevRank})\n**To:** {time.ToString(useHundredths: map.Game.IsTMUF())} (rank: {rank})\n**By:** {lastTop10Change.Login.GetDeformattedNickname()}";
                    }
                }
            }
            else
            {
                var time = new TimeInt32(lastTop10Change.Time.GetValueOrDefault());
                var rank = lastTop10Change.Rank.GetValueOrDefault().ToString();

                activityText = $"{rank}) {time.ToString(useHundredths: map.Game.IsTMUF())} by {lastTop10Change.Login.GetDeformattedNickname()}";
            }
            
            builder.AddField($"Last Top 10 activity  ➡️  {typeOfActivity}", activityText);
            
            return recordSet;
        }

        public override async Task<DiscordBotMessage?> ExecuteButtonAsync(SocketMessageComponent messageComponent, Deferer deferer)
        {
            var split = messageComponent.Data.CustomId.Split('-');

            if (split.Length < 3)
            {
                return new DiscordBotMessage(new EmbedBuilder().WithDescription("Not enough data for the command.").Build(),
                    ephemeral: true, alwaysPostAsNewMessage: true);
            }

            var mapUid = split[2];

            if (messageComponent.Data.CustomId.StartsWith(CreateCustomId("top10")))
            {
                return await ExecuteMapRelatedCommandFromButtonAsync<Top10Command>(messageComponent, deferer, mapUid);
            }
            else if (messageComponent.Data.CustomId.StartsWith(CreateCustomId("wrdetails")))
            {
                return await ExecuteMapRelatedCommandFromButtonAsync<WrCommand>(messageComponent, deferer, mapUid);
            }
            else if (messageComponent.Data.CustomId.StartsWith(CreateCustomId("wrhistory")))
            {
                return await ExecuteMapRelatedCommandFromButtonAsync<HistoryCommand.Wr>(messageComponent, deferer, mapUid);
            }
            else if (messageComponent.Data.CustomId.StartsWith(CreateCustomId("counthistory")))
            {
                return await ExecuteMapRelatedCommandFromButtonAsync<HistoryCommand.RecordCount.Map>(messageComponent, deferer, mapUid);
            }

            return null;
        }

        private async Task<DiscordBotMessage> ExecuteMapRelatedCommandFromButtonAsync<T>(SocketInteraction messageComponent, Deferer deferer, string mapUid) where T : MapRelatedWithUidCommand
        {
            using var scope = _tmwrDiscordBotService.CreateCommand(out T? mapRelatedCommand);

            if (mapRelatedCommand is null)
            {
                throw new Exception();
            }

            mapRelatedCommand.MapUid = mapUid;

            var message = await mapRelatedCommand.ExecuteAsync(messageComponent, deferer);

            return message with { AlwaysPostAsNewMessage = true, Ephemeral = true };
        }
    }
}
