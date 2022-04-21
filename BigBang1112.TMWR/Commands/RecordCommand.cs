using BigBang1112.Extensions;
using BigBang1112.TMWR.Models;
using BigBang1112.WorldRecordReportLib.Enums;
using BigBang1112.WorldRecordReportLib.Models;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;
using BigBang1112.WorldRecordReportLib.Services;
using Discord;
using System.Text.RegularExpressions;
using TmEssentials;

namespace BigBang1112.TMWR.Commands;

[DiscordBotCommand("record", "Shows information about a certain record.")]
public class RecordCommand : MapRelatedWithUidCommand
{
    private readonly IWrRepo _repo;
    private readonly IRecordSetService _recordSetService;
    private readonly ITmxRecordSetService _tmxRecordSetService;

    private RecordSet? recordSet;
    private TmxReplay[]? recordSetTmx;
    private string nickname = "";

    [DiscordBotCommandOption("rank",
        ApplicationCommandOptionType.Integer,
        "Rank of the record in the list.",
        IsRequired = true,
        MinValue = 1)]
    public long Rank { get; set; }

    public RecordCommand(TmwrDiscordBotService tmwrDiscordBotService,
                         IWrRepo repo,
                         IRecordSetService recordSetService,
                         ITmxRecordSetService tmxRecordSetService) : base(tmwrDiscordBotService, repo)
    {
        _repo = repo;
        _recordSetService = recordSetService;
        _tmxRecordSetService = tmxRecordSetService;
    }

    protected override Task<ComponentBuilder?> CreateComponentsAsync(MapModel map, bool isModified)
    {
        var builder = new ComponentBuilder();

        if (map.Game.IsTM2() && recordSet is not null)
        {
            var record = recordSet.Records.ElementAtOrDefault((int)Rank - 1);

            builder = builder.WithButton("Download ghost",
                customId: record is null ? "download-disabled" : null,
                style: record is null ? ButtonStyle.Secondary : ButtonStyle.Link,
                url: record?.ReplayUrl,
                disabled: record is null);
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

        builder.Description = $"{map.GetHumanizedDeformattedName()} by {map.Author.GetDeformattedNickname().EscapeDiscord()}";

        var infoUrl = map.GetInfoUrl();

        if (infoUrl is not null)
        {
            builder.Description = $"[{builder.Description}]({infoUrl})";
        }

        var rec = (WorldRecordReportLib.Enums.Game)map.Game.Id switch
        {
            WorldRecordReportLib.Enums.Game.TM2 => await FindDetailedRecordFromTM2Async(map),
            WorldRecordReportLib.Enums.Game.TMUF => await FindDetailedRecordFromTMUFAsync(map),
            WorldRecordReportLib.Enums.Game.TM2020 => await FindDetailedRecordFromTM2020Async(map),
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

        builder.Title = $"{score} by {nickname.EscapeDiscord()}";

        builder.AddField("Login", rec.Login, inline: true);

        if (rec.DrivenOn.HasValue)
        {
            builder.AddField("Driven on", rec.DrivenOn.Value.UtcDateTime.ToTimestampTag(TimestampTagStyles.LongDateTime), inline: true);
        }
    }

    private async Task<DetailedRecord?> FindDetailedRecordFromTM2Async(MapModel map)
    {
        recordSet = await _recordSetService.GetFromMapAsync("World", map.MapUid);

        if (recordSet is null)
        {
            return null;
        }

        var record = recordSet.Records.ElementAtOrDefault((int)Rank - 1);

        if (record is null)
        {
            return null;
        }

        var loginModel = await _repo.GetLoginAsync(record.Login);

        nickname = loginModel?.GetDeformattedNickname() ?? record.Login;

        return new(record.Rank, record.Time, nickname, record.Login, DrivenOn: null);
    }

    private async Task<DetailedRecord?> FindDetailedRecordFromTMUFAsync(MapModel map)
    {
        if (map.TmxAuthor is null)
        {
            return null;
        }

        recordSetTmx = await _tmxRecordSetService.GetRecordSetAsync(map.TmxAuthor.Site, map);

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
            : new TimeInt32(record.ReplayTime).ToString(useHundredths: map.Game.IsTMUF());

        nickname = record.UserName ?? record.UserId.ToString();

        return new(record.Rank.GetValueOrDefault(), map.IsStuntsMode() ? record.ReplayScore : record.ReplayTime, nickname, record.UserId.ToString(), record.ReplayAt);
    }

    private async Task<DetailedRecord?> FindDetailedRecordFromTM2020Async(MapModel map)
    {
        return null;
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
