using BigBang1112.Extensions;
using BigBang1112.TMWR.Models;
using BigBang1112.WorldRecordReportLib.Data;
using BigBang1112.WorldRecordReportLib.Enums;
using BigBang1112.WorldRecordReportLib.Models;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;
using BigBang1112.WorldRecordReportLib.Services;
using Discord;
using Microsoft.Extensions.Configuration;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using TmEssentials;
using Game = BigBang1112.WorldRecordReportLib.Enums.Game;

namespace BigBang1112.TMWR.Commands;

[DiscordBotCommand("record", "Shows information about a certain record.")]
public class RecordCommand : MapRelatedWithUidCommand
{
    private readonly IWrUnitOfWork _wrUnitOfWork;
    private readonly RecordStorageService _recordStorageService;
    private readonly IConfiguration _config;

    private LeaderboardTM2? recordSet;
    private ReadOnlyCollection<TmxReplay>? recordSetTmx;
    private ReadOnlyCollection<TM2020Record>? tm2020Leaderboard;
    private string nickname = "";

    [DiscordBotCommandOption("rank",
        ApplicationCommandOptionType.Integer,
        "Rank of the record in the list.",
        IsRequired = true,
        MinValue = 1)]
    public long Rank { get; set; }

    public RecordCommand(TmwrDiscordBotService tmwrDiscordBotService,
                         IWrUnitOfWork wrUnitOfWork,
                         RecordStorageService recordStorageService,
                         IConfiguration config) : base(tmwrDiscordBotService, wrUnitOfWork)
    {
        _wrUnitOfWork = wrUnitOfWork;
        _recordStorageService = recordStorageService;
        _config = config;
    }

    protected override Task<ComponentBuilder?> CreateComponentsAsync(MapModel map, bool isModified)
    {
        var builder = new ComponentBuilder();

        if (map.Game.IsTM2() && recordSet is not null)
        {
            var record = recordSet.Records.ElementAtOrDefault((int)Rank - 1);

            if (record is null)
            {
                builder = builder.WithButton("Download ghost",
                    customId: "download-disabled",
                    style: ButtonStyle.Secondary,
                    disabled: true);
            }
            else
            {
                var downloadUrl = $"https://{_config["BaseAddress"]}/api/v1/ghost/download/{map.MapUid}/{record.Time}/{record.Login}";

                builder = builder.WithButton("Download ghost",
                    style: ButtonStyle.Link,
                    url: downloadUrl);
            }
        }
        else if (map.Game.IsTM2020() && tm2020Leaderboard is not null)
        {
            var record = tm2020Leaderboard.Where(x => !x.Ignored).ElementAtOrDefault((int)Rank - 1);

            if (record is null)
            {
                builder = builder.WithButton("Download ghost",
                    customId: "download-disabled",
                    style: ButtonStyle.Secondary,
                    disabled: true);
            }
            else
            {
                var downloadUrl = $"https://{_config["BaseAddress"]}/api/v1/ghost/download/{map.MapUid}/{record.Time.TotalMilliseconds}/{record.PlayerId}";

                builder = builder.WithButton("Download ghost",
                    style: ButtonStyle.Link,
                    url: downloadUrl);
            }
        }
        else if (map.Game.IsTMUF() && recordSetTmx is not null)
        {
            var record = recordSetTmx.Where(x => x.Rank is not null).ElementAtOrDefault((int)Rank - 1);

            builder = builder.WithButton("Download replay",
                customId: record is null ? "download-disabled" : null,
                style: record is null ? ButtonStyle.Secondary : ButtonStyle.Link,
                url: record is not null && map.TmxAuthor is not null ? $"{map.TmxAuthor.Site.Url}recordgbx/{record.ReplayId}" : null,
                disabled: record is null);
        }

        builder = builder.WithButton("Checkpoints", CreateCustomId($"{MapUid}-{Rank}-checkpoints"), ButtonStyle.Secondary, disabled: true)
            .WithButton("Inputs", CreateCustomId($"{MapUid}-{Rank}-inputs"), ButtonStyle.Secondary, disabled: true);

        return Task.FromResult(builder)!;
    }

    protected override async Task BuildEmbedResponseAsync(MapModel map, EmbedBuilder builder)
    {
        builder.Footer.Text = $"({Rank}) {builder.Footer.Text}";
        builder.ThumbnailUrl = map.GetThumbnailUrl();

        builder.Description = $"{map.GetMdLinkHumanized()} by {map.GetAuthorNicknameMdLink()}";

        var rec = (Game)map.Game.Id switch
        {
            Game.TM2 => await FindDetailedRecordFromTM2Async(map),
            Game.TMUF => await FindDetailedRecordFromTMUFAsync(map),
            Game.TM2020 => await FindDetailedRecordFromTM2020Async(map),
            _ => null
        };

        if (rec is null)
        {
            builder.Title = "No record found";
            return;
        }

        var score = map.IsStuntsMode()
            ? rec.TimeOrScore.ToString()
            : new TimeInt32(rec.TimeOrScore).ToString(useHundredths: map.Game.IsTMUF());

        builder.Title = $"{score} by {nickname}";

        var isLoginUnder16Chars = rec.Login.Length < 16;

        var idType = (Game)map.Game.Id switch
        {
            Game.TM2 => "Login",
            Game.TMUF => "User ID",
            Game.TM2020 => "Account ID",
            _ => "Login"
        };

        var login = rec.Login;
            
        if (map.Game.IsTMUF() && map.TmxAuthor is not null)
        {
            login = $"[{rec.Login}]({map.TmxAuthor.Site.Url}usershow/{rec.Login})";
        }

        if (map.Game.IsTM2020())
        {
            login = $"[{rec.Login}](https://trackmania.io/#/player/{rec.Login})";
        }

        builder.AddField(idType, login, inline: isLoginUnder16Chars);

        if (rec.DrivenOn.HasValue)
        {
            builder.AddField("Driven on", rec.DrivenOn.Value.UtcDateTime.ToTimestampTag(TimestampTagStyles.LongDateTime), inline: isLoginUnder16Chars);
        }
    }

    private async Task<DetailedRecord?> FindDetailedRecordFromTM2Async(MapModel map)
    {
        recordSet = await _recordStorageService.GetTM2LeaderboardAsync(map.MapUid);

        if (recordSet is null)
        {
            return null;
        }

        var record = recordSet.Records.ElementAtOrDefault((int)Rank - 1);

        if (record is null)
        {
            return null;
        }

        var loginModel = await _wrUnitOfWork.Logins.GetByNameAsync(Game.TM2, record.Login);

        nickname = loginModel?.GetDeformattedNickname() ?? record.Login;

        return new(record.Rank, record.Time.TotalMilliseconds, nickname, record.Login, DrivenOn: null);
    }

    private async Task<DetailedRecord?> FindDetailedRecordFromTMUFAsync(MapModel map)
    {
        if (map.TmxAuthor is null)
        {
            return null;
        }

        recordSetTmx = await _recordStorageService.GetTmxLeaderboardAsync((TmxSite)map.TmxAuthor.Site.Id, map.MapUid);

        if (recordSetTmx is null)
        {
            return null;
        }

        var record = recordSetTmx.Where(x => x.Rank is not null).ElementAtOrDefault((int)Rank - 1);

        if (record is null)
        {
            return null;
        }

        var score = map.IsStuntsMode()
            ? record.ReplayScore.ToString()
            : record.ReplayTime.ToString(useHundredths: map.Game.IsTMUF());

        nickname = record.UserName?.EscapeDiscord() ?? record.UserId.ToString();

        return new(record.Rank.GetValueOrDefault(), map.IsStuntsMode() ? record.ReplayScore : record.ReplayTime.TotalMilliseconds, nickname, record.UserId.ToString(), record.ReplayAt);
    }

    private async Task<DetailedRecord?> FindDetailedRecordFromTM2020Async(MapModel map)
    {
        tm2020Leaderboard = await _recordStorageService.GetTM2020LeaderboardAsync(map.MapUid);

        if (tm2020Leaderboard is null)
        {
            return null;
        }

        var record = tm2020Leaderboard.Where(x => !x.Ignored).ElementAtOrDefault((int)Rank - 1);

        if (record is null)
        {
            return null;
        }

        var loginModel = await _wrUnitOfWork.Logins.GetByNameAsync(Game.TM2020, record.PlayerId.ToString());

        nickname = loginModel?.GetDeformattedNickname() ?? record.PlayerId.ToString();

        return new(record.Rank, record.Time.TotalMilliseconds, nickname, record.PlayerId.ToString(), record.Timestamp);
    }

    public override Task<DiscordBotMessage?> SelectMenuAsync(SocketMessageComponent messageComponent, Deferer deferrer)
    {
        var footerText = messageComponent.Message.Embeds.FirstOrDefault()?.Footer?.Text;

        if (footerText is null)
        {
            return Task.FromResult(default(DiscordBotMessage));
        }

        var rankMatch = Regex.Match(footerText, "\\((.*?)\\)");

        if (!rankMatch.Success || rankMatch.Groups.Count <= 1)
        {
            return Task.FromResult(default(DiscordBotMessage));
        }

        var rankStr = rankMatch.Groups[1].Value;

        if (!long.TryParse(rankStr, out long rank))
        {
            return Task.FromResult(default(DiscordBotMessage));
        }

        Rank = rank;

        return base.SelectMenuAsync(messageComponent, deferrer);
    }

    public override Task<DiscordBotMessage?> ExecuteButtonAsync(SocketMessageComponent messageComponent, Deferer deferer)
    {
        return base.ExecuteButtonAsync(messageComponent, deferer);
    }
}
