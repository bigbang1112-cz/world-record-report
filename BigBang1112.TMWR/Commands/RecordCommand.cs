using BigBang1112.Extensions;
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
    private string? nickname;

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

        if (map.Game.IsTM2())
        {
            recordSet = await _recordSetService.GetFromMapAsync("World", map.MapUid);

            if (recordSet is null)
            {
                return;
            }

            var record = recordSet.Records.ElementAtOrDefault((int)Rank - 1);

            if (record is null)
            {
                builder.Title = "No record found";
                return;
            }

            var loginModel = await _repo.GetLoginAsync(record.Login);
            
            nickname = loginModel?.GetDeformattedNickname() ?? record.Login;

            builder.Title = $"{record.Rank}) {new TimeInt32(record.Time).ToString(useHundredths: map.Game.IsTMUF())} by {nickname}";
        }
        else if (map.Game.IsTMUF())
        {
            if (map.TmxAuthor is null)
            {
                return;
            }

            recordSetTmx = await _tmxRecordSetService.GetRecordSetAsync(map.TmxAuthor.Site, map);

            if (recordSetTmx is null)
            {
                return;
            }

            var record = recordSetTmx.Where(x => x.Rank is not null).ElementAtOrDefault((int)Rank - 1);

            if (record is null)
            {
                builder.Title = "No record found";
                return;
            }

            var score = map.IsStuntsMode()
                ? record.ReplayScore.ToString()
                : new TimeInt32(record.ReplayTime).ToString(useHundredths: map.Game.IsTMUF());

            nickname = record.UserName ?? record.UserId.ToString();

            builder.Title = $"{record.Rank}) {score} by {nickname}";
        }

        builder.Description = $"{map.GetHumanizedDeformattedName()} by {map.Author.GetDeformattedNickname().EscapeDiscord()}";

        var tmxUrl = map.GetTmxUrl();

        if (tmxUrl is not null)
        {
            builder.Description = $"[{builder.Description}]({tmxUrl})";
        }
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
