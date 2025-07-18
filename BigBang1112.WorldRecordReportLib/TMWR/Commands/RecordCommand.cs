using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using BigBang1112.DiscordBot.Models;
using BigBang1112.WorldRecordReportLib.Data;
using BigBang1112.WorldRecordReportLib.Enums;
using BigBang1112.WorldRecordReportLib.Models;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Services;
using BigBang1112.WorldRecordReportLib.TMWR.Models;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using TmEssentials;
using Game = BigBang1112.WorldRecordReportLib.Enums.Game;

namespace BigBang1112.WorldRecordReportLib.TMWR.Commands;

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

        CreateViewButton(map, builder);
        CreateDownloadButton(map, builder);

        builder = builder.WithButton("Checkpoints", CreateCustomId($"{MapUid}-{Rank}-checkpoints"), ButtonStyle.Secondary, disabled: true)
            .WithButton("Inputs", CreateCustomId($"{MapUid}-{Rank}-inputs"), ButtonStyle.Secondary, disabled: true);

        return Task.FromResult(builder)!;
    }

    private void CreateViewButton(MapModel map, ComponentBuilder builder)
    {
        if (map.Game.IsTM2020())
        {
            return;
        }

        IRecord? rec = (Game)map.Game.Id switch
        {
            Game.TM2 => recordSet?.Records.ElementAtOrDefault((int)Rank - 1),
            Game.TM2020 => tm2020Leaderboard?.Where(x => !x.Ignored).ElementAtOrDefault((int)Rank - 1),
            Game.TMUF or Game.TMN => recordSetTmx?.Where(x => x.Rank is not null).ElementAtOrDefault((int)Rank - 1),
            _ => null
        };

        if (map.Game.IsTM2())
        {
            var viewUrl = $"https://3d.gbx.tools/view/ghost?type=wrr&mapuid={map.MapUid}&time={rec?.Time.TotalMilliseconds}&login={rec?.GetPlayerId()}&mx=TM2";

            builder.WithButton("View ghost", style: ButtonStyle.Link, url: viewUrl);

            return;
        }

        if (map.Game.IsTMUF() || map.Game.IsTMN())
        {
            builder = builder.WithButton("View replay",
                customId: map.TmxAuthor is null ? "view-disabled" : null,
                style: map.TmxAuthor is null ? ButtonStyle.Secondary : ButtonStyle.Link,
                url: map.TmxAuthor is null ? null : $"https://3d.gbx.tools/view/replay?tmx={map.TmxAuthor?.Site.GetSiteEnum()}&id={((TmxReplay?)rec)?.ReplayId}&mapid={map.MxId}",
                disabled: map.TmxAuthor is null);
        }
    }

    private void CreateDownloadButton(MapModel map, ComponentBuilder builder)
    {
        var buttonName = (Game)map.Game.Id switch
        {
            Game.TM2 or Game.TM2020 => "Download ghost",
            _ => "Download replay"
        };

        IRecord? rec = (Game)map.Game.Id switch
        {
            Game.TM2 => recordSet?.Records.ElementAtOrDefault((int)Rank - 1),
            Game.TM2020 => tm2020Leaderboard?.Where(x => !x.Ignored).ElementAtOrDefault((int)Rank - 1),
            Game.TMUF or Game.TMN => recordSetTmx?.Where(x => x.Rank is not null).ElementAtOrDefault((int)Rank - 1),
            _ => null
        };

        if (rec is null)
        {
            builder.WithButton(buttonName,
                customId: "download-disabled",
                style: ButtonStyle.Secondary,
                disabled: true);
            return;
        }

        if (map.Game.IsTM2() || map.Game.IsTM2020())
        {
            if (string.IsNullOrWhiteSpace(_config["BaseAddress"]))
            {
                throw new Exception("BaseAddress is not present in configuration");
            }

            var downloadUrl = $"https://{_config["BaseAddress"]}/api/v1/ghost/download/{map.MapUid}/{rec.Time.TotalMilliseconds}/{rec.GetPlayerId()}";

            builder.WithButton("Download ghost", style: ButtonStyle.Link, url: downloadUrl);

            return;
        }
        
        if (map.Game.IsTMUF() || map.Game.IsTMN())
        {
            builder = builder.WithButton("Download replay",
                customId: map.TmxAuthor is null ? "download-disabled" : null,
                style: map.TmxAuthor is null ? ButtonStyle.Secondary : ButtonStyle.Link,
                url: map.TmxAuthor is null ? null : $"{map.TmxAuthor.Site.Url}recordgbx/{((TmxReplay)rec).ReplayId}",
                disabled: map.TmxAuthor is null);
        }
    }

    protected override async Task BuildEmbedResponseAsync(MapModel map, EmbedBuilder builder)
    {
        builder.Footer.Text = $"({Rank}) {builder.Footer.Text}";
        builder.ThumbnailUrl = map.GetThumbnailUrl();

        builder.Description = $"{map.GetMdLinkHumanized()} by {map.GetAuthorNicknameMdLink()}";

        var rec = (Game)map.Game.Id switch
        {
            Game.TM2 => await FindDetailedRecordFromTM2Async(map),
            Game.TMUF or Game.TMN => await FindDetailedRecordFromTMUFAsync(map),
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

        if (!map.Game.IsTM2020())
        {
            var recTmx = (Game)map.Game.Id switch
            {
                Game.TMUF or Game.TMN => recordSetTmx?.Where(x => x.Rank is not null).ElementAtOrDefault((int)Rank - 1),
                _ => null
            };

            builder.Url = map.Game.IsTM2()
                ? $"https://3d.gbx.tools/view/ghost?type=wrr&mapuid={map.MapUid}&time={rec.TimeOrScore}&login={rec.Login}&mx=TM2"
                : $"https://3d.gbx.tools/view/replay?tmx={map.TmxAuthor?.Site.GetSiteEnum()}&id={recTmx?.ReplayId}&mapid={map.MxId}";
        }

        var isLoginUnder16Chars = rec.Login.Length < 16;

        var idType = (Game)map.Game.Id switch
        {
            Game.TM2 => "Login",
            Game.TMUF or Game.TMN => "User ID",
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

        return new(record.Rank, record.Time.TotalMilliseconds, nickname, record.Login, record.Timestamp);
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
