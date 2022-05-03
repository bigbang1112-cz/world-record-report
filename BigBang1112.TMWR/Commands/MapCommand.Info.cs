using System.Text;
using BigBang1112.Extensions;
using BigBang1112.WorldRecordReportLib.Data;
using BigBang1112.WorldRecordReportLib.Enums;
using BigBang1112.WorldRecordReportLib.Models;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;
using BigBang1112.WorldRecordReportLib.Services;
using Discord;
using TmEssentials;

using Game = BigBang1112.WorldRecordReportLib.Enums.Game;

namespace BigBang1112.TMWR.Commands;

public partial class MapCommand
{
    [DiscordBotSubCommand("info", "Gets information about the map.")]
    public class Info : MapRelatedWithUidCommand
    {
        private readonly TmwrDiscordBotService _tmwrDiscordBotService;
        private readonly RecordStorageService _recordStorageService;
        private readonly IWrUnitOfWork _wrUnitOfWork;

        public Info(TmwrDiscordBotService tmwrDiscordBotService, RecordStorageService recordStorageService, IWrUnitOfWork wrUnitOfWork) : base(tmwrDiscordBotService, wrUnitOfWork)
        {
            _tmwrDiscordBotService = tmwrDiscordBotService;
            _recordStorageService = recordStorageService;
            _wrUnitOfWork = wrUnitOfWork;
        }

        protected override Task<ComponentBuilder?> CreateComponentsAsync(MapModel map, bool isModified)
        {
            var builder = new ComponentBuilder()
                .WithButton("Top 10", CreateCustomId($"top10-{map.MapUid}"), ButtonStyle.Secondary, disabled: false)
                .WithButton("World record", CreateCustomId($"wrdetails-{map.MapUid}"), ButtonStyle.Secondary, disabled: false)
                .WithButton("World record history", CreateCustomId($"wrhistory-{map.MapUid}"), ButtonStyle.Secondary, disabled: false)
                .WithButton("Record count history", CreateCustomId($"counthistory-{map.MapUid}"), ButtonStyle.Secondary, disabled: true);

            return Task.FromResult(builder)!;
        }

        protected override async Task BuildEmbedResponseAsync(MapModel map, EmbedBuilder builder)
        {
            builder.Title = $"{map.GetHumanizedDeformattedName()} by {map.Author.GetDeformattedNickname().EscapeDiscord()}";
            builder.ThumbnailUrl = map.GetThumbnailUrl();
            builder.Url = map.GetInfoUrl();
            builder.Timestamp = DateTimeOffset.UtcNow;

            builder.Description = string.Join(" | ", EnumerateLinks(map));

            if (map.TitlePack is null)
            {
                builder.AddField("Game", map.IntendedGame ?? map.Game, inline: true);
            }
            else
            {
                builder.AddField("Game / Title pack", map.TitlePack, inline: true);
            }

            builder.AddField("Environment", map.Environment, inline: true);

            if (map.Campaign is not null)
            {
                builder.AddField("Campaign", map.Campaign.Name, inline: true);
            }

            var wr = await _wrUnitOfWork.WorldRecords.GetCurrentByMapAsync(map);

            if (wr is not null)
            {
                builder.AddField("World record",
                    $"**` {wr.GetTimeFormattedToGame()} `** by **{wr.GetPlayerNicknameMdLink()}**");

                builder.AddField("World record driven on", wr.DrivenOn.ToTimestampTag(TimestampTagStyles.LongDateTime));
            }

            if (map.LastActivityOn.HasValue)
            {
                builder.AddField("Last activity", (map.TmxAuthor is null ? "" : "**TMX:** ") + map.LastActivityOn.Value.ToTimestampTag(TimestampTagStyles.Relative), inline: true);
            }

            var lastTop10Change = await _wrUnitOfWork.RecordSetDetailedChanges.GetLatestByMapAsync(map);

            var records = default(IEnumerable<IRecord>);
            var recordCount = default(int?);

            var game = (Game)map.Game.Id;

            switch (game)
            {
                case Game.TM2:
                    var lb = await _recordStorageService.GetTM2LeaderboardAsync(map.MapUid);
                    records = lb?.Records;
                    recordCount = lb?.GetRecordCount();
                    break;
                case Game.TM2020:
                    records = await _recordStorageService.GetTM2020LeaderboardAsync(map.MapUid);
                    break;
                case Game.TMUF:
                    if (map.TmxAuthor is not null)
                    {
                        records = await _recordStorageService.GetTmxLeaderboardAsync((TmxSite)map.TmxAuthor.Site.Id, map.MapUid);
                    }
                    break;
            }

            if (lastTop10Change is not null)
            {
                await AddLastTop10ActivityAsync(map, builder, lastTop10Change, records);
            }
            else if (game == Game.TMUF)
            {
                AddLastTop10ActivityTmx(map, builder, records);
            }

            if (recordCount.HasValue)
            {
                builder.AddField("Record count", recordCount.Value.ToString("N0"));
            }

            if (map.FileLastModifiedOn.HasValue)
            {
                builder.AddField("Last modified on", map.FileLastModifiedOn.Value.ToTimestampTag());
            }
        }

        private static IEnumerable<string> EnumerateLinks(MapModel map)
        {
            if (!map.Game.IsTM2020())
            {
                yield break;
            }

            var tmxUrl = map.GetTmxUrl();

            if (tmxUrl is not null)
            {
                yield return $"[TMX]({tmxUrl})";
            }

            var tmIoUrl = map.GetTrackmaniaIoUrl();

            if (tmIoUrl is not null)
            {
                yield return $"[TM.IO]({tmIoUrl})";
            }
        }

        private async Task AddLastTop10ActivityAsync(MapModel map,
                                                     EmbedBuilder builder,
                                                     RecordSetDetailedChangeModel lastTop10Change,
                                                     IEnumerable<IRecord>? records)
        {
            var fieldName = (Game)map.Game.Id switch
            {
                Game.TM2 => "Last Top 10 activity",
                Game.TM2020 => "Last Top 20 activity",
                _ => "Last activity"
            };

            if (lastTop10Change.DrivenBefore.HasValue)
            {
                var oldestChange = (await _wrUnitOfWork.RecordSetDetailedChanges.GetOldestByMapAsync(map))?.DrivenBefore;

                var drivenBefore = lastTop10Change.DrivenBefore.Value;

                var timestampTag = drivenBefore.ToTimestampTag(TimestampTagStyles.Relative).ToString();

                if (oldestChange is not null && drivenBefore - oldestChange.Value < TimeSpan.FromDays(1))
                {
                    timestampTag += "+";
                }

                builder.AddField(fieldName, timestampTag, inline: true);
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

            var activityText = "No details available";

            switch (lastTop10Change.Type)
            {
                case RecordSetDetailedChangeType.New:
                    {
                        if (records is null)
                        {
                            break;
                        }

                        var record = records.FirstOrDefault(x => x.GetPlayerId() == lastTop10Change.Login.Name);

                        if (record is not null)
                        {
                            var time = record.Time;
                            var rank = record.Rank;

                            activityText = $"` {rank} ` **` {time.ToString(useHundredths: map.Game.IsTMUF())} `** by **{lastTop10Change.Login.GetMdLink()}**";
                        }

                        break;
                    }

                case RecordSetDetailedChangeType.Improvement:
                    {
                        var prevTime = new TimeInt32(lastTop10Change.Time.GetValueOrDefault());
                        var prevRank = lastTop10Change.Rank.GetValueOrDefault().ToString();

                        if (records is null)
                        {
                            activityText = $"{prevTime.ToString(useHundredths: map.Game.IsTMUF())} (rank: {prevRank}) to [unknown] by **{lastTop10Change.Login.GetMdLink()}**";
                            break;
                        }

                        var record = records.FirstOrDefault(x => x.GetPlayerId() == lastTop10Change.Login.Name);

                        if (record is not null)
                        {
                            var time = record.Time;
                            var rank = record.Rank;

                            activityText = $"From: **` {prevTime.ToString(useHundredths: map.Game.IsTMUF())} `** (rank: ` {prevRank} `)\n" +
                                           $"To: **` {time.ToString(useHundredths: map.Game.IsTMUF())} `** (rank: ` {rank} `)\n" +
                                           $"By: **{lastTop10Change.Login.GetMdLink()}**";
                        }

                        break;
                    }

                default:
                    {
                        var time = new TimeInt32(lastTop10Change.Time.GetValueOrDefault());
                        var rank = lastTop10Change.Rank.GetValueOrDefault().ToString();

                        activityText = $"` {rank} ` **` {time.ToString(useHundredths: map.Game.IsTMUF())} `** by **{lastTop10Change.Login.GetMdLink()}**";
                        break;
                    }
            }

            builder.AddField($"{fieldName}  ➡️  {typeOfActivity}", activityText);
        }

        private void AddLastTop10ActivityTmx(MapModel map, EmbedBuilder builder, IEnumerable<IRecord>? records)
        {
            if (records is null || !records.Any())
            {
                return;
            }

            var tmxRecords = records.Cast<TmxReplay>();
            var newestRecord = tmxRecords.OrderByDescending(x => x.ReplayAt).Where(x => x.Rank <= 10).First();

            builder.AddField("Last Top 10 activity", "**TMX:** " + newestRecord.ReplayAt.ToTimestampTag(TimestampTagStyles.Relative), inline: true);

            var recordsByNewestRecordUser = tmxRecords.Where(x => x.UserId == newestRecord.UserId).Take(2);

            var olderRecord = recordsByNewestRecordUser.ElementAtOrDefault(1);

            if (newestRecord.Rank is null)
            {
                throw new Exception("Rank is null even though this should be a ranked record");
            }

            var activityText = olderRecord is null
                ? $"` {newestRecord.Rank.Value} ` **` {newestRecord.ReplayTime.ToString(useHundredths: map.Game.IsTMUF())} `** by **{newestRecord.GetDisplayNameMdLink()}**"
                : $"From: **` {olderRecord.ReplayTime.ToString(useHundredths: map.Game.IsTMUF())} `**\n" +
                  $"To: **` {newestRecord.ReplayTime.ToString(useHundredths: map.Game.IsTMUF())} `** (rank: ` {newestRecord.Rank} `)\n" +
                  $"By: **{newestRecord.GetDisplayNameMdLink()}**";
            
            var typeOfActivity = olderRecord is null ? "New record" : "Improved record";

            builder.AddField($"Last Top 10 TMX activity  ➡️  {typeOfActivity}", activityText);
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
