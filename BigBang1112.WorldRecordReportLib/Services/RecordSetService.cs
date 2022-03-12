using BigBang1112.WorldRecordReportLib.Comparers;
using BigBang1112.WorldRecordReportLib.Converters.Json;
using BigBang1112.WorldRecordReportLib.Models;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Text.Json;
using TmEssentials;

namespace BigBang1112.WorldRecordReportLib.Services;

public class RecordSetService : IRecordSetService
{
    public static readonly string StorageFolder = Path.Combine("api", "v1", "records", "tm2");

    private readonly ILogger<RecordSetService> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly IWrRepo _repo;
    private readonly IMemoryCache _cache;
    private readonly IDiscordWebhookService _discordWebhookService;
    private readonly IGhostService _ghostService;

    private static readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    static RecordSetService()
    {
        jsonSerializerOptions.Converters.Add(new RecordSetTimesConverter());
    }

    public RecordSetService(ILogger<RecordSetService> logger,
                            IWebHostEnvironment env,
                            IWrRepo repo,
                            IMemoryCache cache,
                            IDiscordWebhookService discordWebhookService,
                            IGhostService ghostService)
    {
        _logger = logger;
        _env = env;
        _repo = repo;
        _cache = cache;
        _discordWebhookService = discordWebhookService;
        _ghostService = ghostService;
    }

    public void GetFilePaths(string zone, string mapUid,
        out string path, out string fileName, out string fullFileName, out string subDirFileName)
    {
        path = Path.Combine(_env.WebRootPath, StorageFolder, zone);
        fileName = $"{mapUid}.json.gz";
        fullFileName = Path.Combine(path, fileName);
        subDirFileName = Path.Combine(StorageFolder, zone, fileName);
    }

    public IFileInfo GetFileInfo(string zone, string mapUid)
    {
        GetFilePaths(zone, mapUid,
            out string _,
            out string _,
            out string _,
            out string subDirFileName);

        return _env.WebRootFileProvider.GetFileInfo(subDirFileName);
    }

    public async Task UpdateRecordSetAsync(string zone, string mapUid, RecordSet recordSet)
    {
        GetFilePaths(zone, mapUid,
            out string path,
            out string _,
            out string fullFileName,
            out string subDirFileName);

        Directory.CreateDirectory(path);

        var file = _env.WebRootFileProvider.GetFileInfo(subDirFileName);

        if (!file.Exists)
        {
            await SaveRecordSetAsync(fullFileName, recordSet);
            return;
        }

        RecordSet recordSetPrev;

        using (var stream = File.OpenRead(fullFileName))
        using (var gzip = new GZipStream(stream, CompressionMode.Decompress))
        {
            recordSetPrev = await JsonHelper.DeserializeAsync<RecordSet>(gzip, jsonSerializerOptions);
        }

        await DownloadMissingGhostsAsync(mapUid, recordSet);

        var recordSetChanges = CompareTop10(recordSet, recordSetPrev);
        var timeChanges = CompareTimes(recordSet, recordSetPrev);

        if (timeChanges is not null && timeChanges.NewRecords.Count > 0)
        {
            _logger.LogInformation("New records on {Map}: {Records}", mapUid,
                string.Join(", ", timeChanges.NewRecords.Select(x => $"{new TimeInt32(x.Key)} {x.Value}x")));
        }

        var map = await _repo.GetMapByUidAsync(mapUid);

        if (map is null)
        {
            return;
        }

        var noCountExists = await _repo.HasRecordCountAsync(map);

        if (noCountExists)
        {
            await SaveRecordCountToDatabaseAsync(File.GetLastWriteTimeUtc(fullFileName), map, recordSetPrev.GetRecordCount());
        }

        var cacheKey = $"RecordSet_{mapUid}";

        if (timeChanges is not null)
        {
            await ApplyChangesAsync(recordSet, fullFileName, recordSetChanges, timeChanges, cacheKey, map, noCountExists);
        }

        _cache.Set(cacheKey, DateTime.UtcNow);
    }

    private async Task DownloadMissingGhostsAsync(string mapUid, RecordSet recordSet)
    {
        foreach (var record in recordSet.Records)
        {
            if (_ghostService.GhostExists(mapUid, record))
            {
                continue;
            }

            await _ghostService.DownloadGhostAndGetTimestampAsync(mapUid, record);

            _logger.LogInformation("Downloaded {time} on {mapUid} by {login}", new TimeInt32(record.Time), mapUid, record.Login);

            await Task.Delay(500);
        }
    }

    private async Task ApplyChangesAsync(RecordSet recordSet, string fullFileName, RecordSetDetailedRecordChanges? recordSetChanges, RecordSetChanges timeChanges, string cacheKey, MapModel map, bool noCountExists)
    {
        var drivenAfter = _cache.GetOrCreate(cacheKey, entry =>
        {
            return File.GetLastWriteTimeUtc(fullFileName);
        });

        await SaveRecordSetAsync(fullFileName, recordSet);

        var drivenBefore = File.GetLastWriteTimeUtc(fullFileName);

        if (!noCountExists)
        {
            await SaveRecordCountToDatabaseAsync(drivenBefore, map, recordSet.GetRecordCount());
        }

        await SaveChangesToDatabaseAsync(drivenBefore, map, drivenAfter, timeChanges);

        if (recordSetChanges is null)
        {
            return;
        }

        var game = await _repo.GetTM2GameAsync();

        var logins = new Dictionary<string, LoginModel>();
        var changes = new List<RecordSetDetailedChangeModel>();

        var allPossibleRecordChanges = new Dictionary<RecordSetDetailedChangeType, IEnumerable<RecordSetDetailedRecord>>
        {
            { RecordSetDetailedChangeType.New, recordSetChanges.NewRecords.Select(login => new RecordSetDetailedRecord(rank: 0, login, time: 0)) },
            { RecordSetDetailedChangeType.Improvement, recordSetChanges.ImprovedRecords },
            { RecordSetDetailedChangeType.Removed, recordSetChanges.RemovedRecords },
            { RecordSetDetailedChangeType.Worsen, recordSetChanges.WorsenRecords },
            { RecordSetDetailedChangeType.PushedOff, recordSetChanges.PushedOffRecords }
        };

        foreach (var (changeType, recordChanges) in allPossibleRecordChanges)
        {
            foreach (var record in recordChanges)
            {
                if (!logins.ContainsKey(record.Login))
                {
                    logins[record.Login] = await _repo.GetOrAddLoginAsync(record.Login, game);
                }

                var recordChange = new RecordSetDetailedChangeModel
                {
                    Login = logins[record.Login],
                    Map = map,
                    Type = changeType,
                    DrivenBefore = drivenBefore
                };

                if (changeType != RecordSetDetailedChangeType.New)
                {
                    recordChange.Rank = record.Rank;
                    recordChange.Time = record.Time;
                    recordChange.ReplayUrl = record.ReplayUrl;
                }

                changes.Add(recordChange);
            }
        }

        await _repo.AddRecordSetDetailedChangesAsync(changes);

        await ReportRemovedRecordsAsync(changes);

        await _repo.SaveAsync();
    }

    private async Task ReportRemovedRecordsAsync(IEnumerable<RecordSetDetailedChangeModel> changes)
    {
        var removedRecs = changes.Where(x => x.Type == RecordSetDetailedChangeType.Removed);

        if (!removedRecs.Any())
            return;

        var ignoredLoginsFromRemovedRecordReport = await _repo.GetIgnoredLoginsFromRemovedRecordReportAsync();

        foreach (var webhook in await _repo.GetDiscordWebhooksAsync())
        {
            if (!Uri.IsWellFormedUriString(webhook.Url, UriKind.Absolute))
                continue;

            if (!string.IsNullOrWhiteSpace(webhook.Filter))
            {
                try
                {
                    var filter = JsonHelper.Deserialize<DiscordWebhookFilter>(webhook.Filter);

                    if (filter.ReportRemovedRecsFromTM2 != true)
                    {
                        continue;
                    }
                }
                catch
                {
                    continue;
                }
            }
            else
            {
                // Allows everything
            }

            var embeds = new List<Discord.Embed>();

            foreach (var rec in removedRecs)
            {
                if (ignoredLoginsFromRemovedRecordReport.Contains(rec.Login.Name))
                    continue;

                var embed = new Discord.EmbedBuilder()
                    .WithTitle("Removed record detected")
                    .WithDescription($"{rec.Map.Environment} {rec.Map.DeformattedName}: {new TimeInt32(rec.Time.GetValueOrDefault())} by {TextFormatter.Deformat(rec.Login.ToString())}")
                    .Build();

                embeds.Add(embed);
            }

            if (embeds.Count > 0)
            {
                _ = await _discordWebhookService.SendMessageAsync(webhook, embeds: embeds);
            }
        }
    }

    private async Task SaveRecordCountToDatabaseAsync(DateTime drivenBefore, MapModel map, int count)
    {
        var recordCount = new RecordCountModel
        {
            Map = map,
            Count = count,
            Before = drivenBefore
        };

        await _repo.AddRecordCountAsync(recordCount);
        await _repo.SaveAsync();
    }

    private async Task SaveChangesToDatabaseAsync(DateTime drivenBefore, MapModel map, DateTime drivenAfter, RecordSetChanges timeChanges)
    {
        var recordSetChange = new RecordSetChangeModel
        {
            DrivenAfter = drivenAfter,
            DrivenBefore = drivenBefore,
            Map = map
        };

        await _repo.AddRecordSetChangeAsync(recordSetChange);

        var changesNewRecords = timeChanges.NewRecords.Select(rec => new RecordChangeModel
        {
            NewRecord = true,
            Time = rec.Key,
            Count = (short)rec.Value,
            RecordSetChange = recordSetChange
        });

        // add removed records support

        await _repo.AddRecordChangesAsync(changesNewRecords);
        await _repo.SaveAsync();
    }

    public Stream? GetStreamFromMap(string zone, string mapUid)
    {
        if (string.IsNullOrWhiteSpace(zone))
        {
            throw new ArgumentException("Zone cannot be null or white space", nameof(zone));
        }

        if (string.IsNullOrWhiteSpace(mapUid))
        {
            throw new ArgumentException("Map UID cannot be null or white space", nameof(mapUid));
        }

        GetFilePaths(zone, mapUid,
            out string _,
            out string _,
            out string fullFileName,
            out string subDirFileName);

        var file = _env.WebRootFileProvider.GetFileInfo(subDirFileName);

        if (!file.Exists)
        {
            return null;
        }

        var stream = File.OpenRead(fullFileName);
        var gzip = new GZipStream(stream, CompressionMode.Decompress);

        return gzip;
    }

    public async Task<RecordSet?> GetFromMapAsync(string zone, string mapUid)
    {
        using var stream = GetStreamFromMap(zone, mapUid);

        if (stream is null)
        {
            return null;
        }

        return await JsonHelper.DeserializeAsync<RecordSet>(stream, jsonSerializerOptions);
    }

    public async Task<string?> GetJsonFromMapAsync(string zone, string mapUid)
    {
        using var stream = GetStreamFromMap(zone, mapUid);

        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);

        return await reader.ReadToEndAsync();
    }

    private static async Task SaveRecordSetAsync(string fullFileName, RecordSet recordSet)
    {
        using var stream = File.Open(fullFileName, FileMode.Create);
        using var gzip = new GZipStream(stream, CompressionMode.Compress);
        await JsonSerializer.SerializeAsync(gzip, recordSet, jsonSerializerOptions);
    }

    private static RecordSetChanges? CompareTimes(RecordSet recordSet, RecordSet recordSetPrev)
    {
        using var newTimesEnumerator = recordSet.Times.GetEnumerator();
        using var prevTimesEnumerator = recordSetPrev.Times.GetEnumerator();

        var newTotalRecordCount = recordSet.Times.Sum(x => x.count);
        var prevTotalRecordCount = recordSetPrev.Times.Sum(x => x.count);

        var mapRecordCountDifference = newTotalRecordCount - prevTotalRecordCount;

        var newRecords = new Dictionary<int, int>();
        var removedRecords = new Dictionary<int, int>();

        var somethingChanged = false;

        while (newTimesEnumerator.MoveNext() && prevTimesEnumerator.MoveNext())
        {
            while (true)
            {
                var (newUniqueTime, newTimeCount) = newTimesEnumerator.Current;
                var (prevUniqueTime, prevTimeCount) = prevTimesEnumerator.Current;

                // Fresh new record/s have appeared
                if (newUniqueTime < prevUniqueTime)
                {
                    somethingChanged = true;

                    var amountOfNewTimes = newTimeCount;

                    newRecords.Add(newUniqueTime, amountOfNewTimes);

                    if (newTimesEnumerator.MoveNext())
                        continue;
                }

                // One or more times have been removed and there's no other record with this time
                if (newUniqueTime > prevUniqueTime)
                {
                    somethingChanged = true;

                    var amountOfRemovedTimes = prevTimeCount;

                    if (mapRecordCountDifference < 0)
                    {
                        // Possibly removed

                        removedRecords.Add(prevUniqueTime, amountOfRemovedTimes);
                    }
                    else
                    {
                        // Possibly improvement
                    }

                    if (prevTimesEnumerator.MoveNext())
                        continue;
                }

                // New existing time/s
                if (newTimeCount > prevTimeCount)
                {
                    somethingChanged = true;

                    newRecords[newUniqueTime] = newTimeCount - prevTimeCount;

                    //newUniqueTime
                }

                // Removed time/s while at least one equal time still exists
                // Or the time has been improved by the same person
                if (newTimeCount < prevTimeCount)
                {
                    somethingChanged = true;

                    //prevUniqueTime

                    if (mapRecordCountDifference < 0)
                    {
                        // The record has been possibly removed

                        removedRecords[prevUniqueTime] = prevTimeCount - newTimeCount;
                    }
                    else
                    {
                        // Unpredictable behaviour, better to not investigate
                        // An improvement or removed record while someone has driven a fresh record
                    }
                }

                break;
            }
        }

        if (!somethingChanged)
        {
            return null;
        }

        return new RecordSetChanges(newRecords, removedRecords);
    }

    internal static RecordSetDetailedRecordChanges? CompareTop10(RecordSet recordSet, RecordSet recordSetPrev)
    {
        return CompareTop10(recordSet.Records, recordSetPrev.Records);
    }

    /// <summary>
    /// Compares two sets of records and returns their differences.
    /// </summary>
    /// <param name="records">Current record set.</param>
    /// <param name="recordsPrev">Previous record set.</param>
    internal static RecordSetDetailedRecordChanges? CompareTop10(
        IEnumerable<RecordSetDetailedRecord> records,
        IEnumerable<RecordSetDetailedRecord> recordsPrev)
    {
        // Compares equality based on everything except rank
        var changesComparer = new RecordSetDetailedRecordChangesComparer();

        // Returns all of the changed records after the leaderboard update
        var currentRecordsWithoutPrevRecords = records.Except(recordsPrev, changesComparer);

        // Returns all of the changed records but as they were before the leaderboard update
        var prevRecordsWithoutCurrentRecords = recordsPrev.Except(records, changesComparer);

        // If there are no changed records
        if (!currentRecordsWithoutPrevRecords.Any() && !prevRecordsWithoutCurrentRecords.Any())
        {
            return null; // somethingChanged is false
        }

        // Compares equality based on the login of the record
        var loginComparer = new RecordSetDetailedRecordLoginComparer();

        var newCurrentRecords = currentRecordsWithoutPrevRecords.Except(prevRecordsWithoutCurrentRecords, loginComparer);
        var removedOrPushedOffRecords = prevRecordsWithoutCurrentRecords.Except(currentRecordsWithoutPrevRecords, loginComparer);

        // There's no previous record that can be detected, so only the login is needed
        var newRecords = newCurrentRecords.Select(x => x.Login);

        var lastRecord = records.Last();

        // Use the last record value to figure out if the record was actually removed or just pushed off Top 10
        var removedRecords = removedOrPushedOffRecords.Where(record => record.Time < lastRecord.Time);
        var pushedOffRecords = removedOrPushedOffRecords.Where(record => record.Time >= lastRecord.Time);

        // Finds an intersection of logins in the Top 10, resulting in either improvement or (unlikely) worse record
        var previousRecordsThatAreImprovedOrWorsen = prevRecordsWithoutCurrentRecords
            .Intersect(currentRecordsWithoutPrevRecords, loginComparer);

        // Compares both records driven by the same login if their time is better than before
        var previousRecordsThatAreImproved = previousRecordsThatAreImprovedOrWorsen
            .WhereWith(currentRecordsWithoutPrevRecords,
                whenMatching: x => x.Login,
                andApplies: (prevRec, currentRec) => currentRec.Time < prevRec.Time);

        // Compares both records driven by the same login if their time is worse than before
        var previousRecordsThatAreWorsen = previousRecordsThatAreImprovedOrWorsen
            .WhereWith(currentRecordsWithoutPrevRecords,
                whenMatching: x => x.Login,
                andApplies: (prevRec, currentRec) => currentRec.Time > prevRec.Time);

        var improvedRecords = previousRecordsThatAreImproved;
        var worsenRecords = previousRecordsThatAreWorsen;

        return new RecordSetDetailedRecordChanges(newRecords, improvedRecords, removedRecords, worsenRecords, pushedOffRecords);
    }
}
