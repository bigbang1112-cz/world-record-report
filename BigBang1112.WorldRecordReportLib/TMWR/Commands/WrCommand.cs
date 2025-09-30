using BigBang1112.DiscordBot.Models;
using BigBang1112.WorldRecordReportLib.TMWR.Extensions;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

using Game = BigBang1112.WorldRecordReportLib.Enums.Game;

namespace BigBang1112.WorldRecordReportLib.TMWR.Commands;

[DiscordBotCommand("wr", "Gets the world record of a map.")]
public class WrCommand : MapRelatedWithUidCommand
{
    private readonly TmwrDiscordBotService _tmwrDiscordBotService;
    private readonly IWrUnitOfWork _wrUnitOfWork;
    private readonly IConfiguration _config;

    [DiscordBotCommandOption("guid",
        ApplicationCommandOptionType.String,
        "GUID of the world record.")]
    public string? Guid { get; set; }

    public async Task<IEnumerable<string>> AutocompleteGuidAsync(string value)
    {
        return await _wrUnitOfWork.WorldRecords.GetAllGuidsLikeAsync(value);
    }

    public WrCommand(TmwrDiscordBotService tmwrDiscordBotService, IWrUnitOfWork wrUnitOfWork, IConfiguration config) : base(tmwrDiscordBotService, wrUnitOfWork)
    {
        _tmwrDiscordBotService = tmwrDiscordBotService;
        _wrUnitOfWork = wrUnitOfWork;
        _config = config;
    }

    public override async Task<DiscordBotMessage> ExecuteAsync(SocketInteraction slashCommand, Deferer deferer)
    {
        if (Guid is null)
        {
            return await base.ExecuteAsync(slashCommand, deferer);
        }

        var wr = await _wrUnitOfWork.WorldRecords.GetByGuidAsync(new Guid(Guid));

        if (wr is null)
        {
            return new DiscordBotMessage(new EmbedBuilder().WithDescription("World record not found.").Build(),
                ephemeral: true, alwaysPostAsNewMessage: true);
        }

        return await CreateResponseMessageWithMapsParamAsync(Enumerable.Repeat(wr.Map, 1), deferer);
    }

    protected override async Task<ComponentBuilder?> CreateComponentsAsync(MapModel map, bool isModified)
    {
        var wr = Guid is null
            ? await _wrUnitOfWork.WorldRecords.GetCurrentByMapAsync(map)
            : await _wrUnitOfWork.WorldRecords.GetByGuidAsync(new Guid(Guid));

        if (wr is null)
        {
            return null;
        }

        var builder = new ComponentBuilder();

        if (map.Game.IsTM2() || map.Game.IsTM2020())
        {
            var downloadUrl = $"https://{_config["BaseAddress"]}/api/v1/ghost/download/{map.MapUid}/{wr.Time}/{wr.GetPlayerLogin()}";

            builder = builder.WithButton("Download ghost",
                customId: downloadUrl is null ? "download-disabled" : null,
                style: downloadUrl is null ? ButtonStyle.Secondary : ButtonStyle.Link,
                url: downloadUrl,
                disabled: downloadUrl is null);
        }
        else if (map.Game.IsTMUF() || map.Game.IsTMN())
        {
            builder = builder.WithButton("Download replay",
                customId: wr.ReplayId is null ? "download-disabled" : null,
                style: wr.ReplayId is null ? ButtonStyle.Secondary : ButtonStyle.Link,
                url: wr.ReplayId is not null && map.TmxAuthor is not null ? $"{map.TmxAuthor.Site.Url}recordgbx/{wr.ReplayId}" : null,
                disabled: wr.ReplayId is null);
        }

        builder = builder
            .WithButton("Previous", CreateCustomId($"{wr.Guid.ToString().Replace('-', '_')}-prev"), ButtonStyle.Secondary)
            .WithButton("Compare with previous", CreateCustomId($"{wr.Guid.ToString().Replace('-', '_')}-compareprev"), ButtonStyle.Secondary)
            .WithButton("Nickname history", CreateCustomId($"{wr.Guid.ToString().Replace('-', '_')}-nicknames"), ButtonStyle.Secondary);

        return builder;
    }

    protected override async Task BuildEmbedResponseAsync(MapModel map, EmbedBuilder builder)
    {
        var wr = Guid is null
            ? await _wrUnitOfWork.WorldRecords.GetCurrentByMapAsync(map)
            : await _wrUnitOfWork.WorldRecords.GetByGuidAsync(new Guid(Guid));

        var thumbnailUrl = map.GetThumbnailUrl();

        if (thumbnailUrl is not null)
        {
            builder.ThumbnailUrl = thumbnailUrl;
        }

        builder.Title = wr is null
            ? "No world record!"
            : $"{wr.GetTimeFormattedToGame()} by {wr.GetPlayerNicknameDeformatted().EscapeDiscord()}{(wr.Unverified ? " *(unverified)*" : "")}";

        builder.Description = $"{map.GetMdLinkHumanized()} by {map.GetAuthorNicknameMdLink()}";

        if (wr is null)
        {
            builder.Footer = null;
            return;
        }

        if (!wr.Map.Game.IsTM2020())
        {
            builder.Url = wr.GetViewUrl();
        }

        var login = wr.GetPlayerLogin();

        var isLoginUnder16Chars = login.Length < 16;

        var idType = (Game)map.Game.Id switch
        {
            Game.TM2 => "Login",
            Game.TMUF or Game.TMN => "User ID",
            Game.TM2020 => "Account ID",
            _ => "Login"
        };

        var infoUrl = wr.GetPlayerInfoUrl();

        if (infoUrl is not null)
        {
            login = $"[{login}]({infoUrl})";
        }

        builder.AddField(idType, login, inline: isLoginUnder16Chars);

        builder.AddField("Driven on", wr.DrivenOn.ToTimestampTag(TimestampTagStyles.LongDateTime), inline: isLoginUnder16Chars);


        var age = DateTime.UtcNow - wr.PublishedOn;

        var nextWr = await _wrUnitOfWork.WorldRecords.GetNextAsync(wr);

        if (nextWr is not null)
        {
            age = nextWr.PublishedOn - wr.PublishedOn;
        }

        builder.AddField("Age", $"{(int)age.TotalDays} days, {age.Hours} hours, {age.Minutes} minutes");

        builder.WithBotFooter(wr.Guid.ToString());
    }

    public override async Task<DiscordBotMessage?> ExecuteButtonAsync(SocketMessageComponent messageComponent, Deferer deferer)
    {
        var split = messageComponent.Data.CustomId.Split('-');

        if (split.Length < 3)
        {
            return new DiscordBotMessage(new EmbedBuilder().WithDescription("Not enough data for the command.").Build(),
                ephemeral: true, alwaysPostAsNewMessage: true);
        }

        var wrGuid = new Guid(split[1].Replace('_', '-'));
        var wr = await _wrUnitOfWork.WorldRecords.GetByGuidAsync(wrGuid);

        if (wr is null)
        {
            return new DiscordBotMessage(new EmbedBuilder().WithDescription("No world record found.").Build(),
                ephemeral: true, alwaysPostAsNewMessage: true);
        }

        return split[2] switch
        {
            "prev" => await ExecutePrevAsync(messageComponent, deferer, wr),
            "compareprev" => await ExecuteComparePrevAsync(messageComponent, deferer, wr),
            "nicknames" => await ExecuteNicknameHistoryAsync(messageComponent, deferer, wr),
            _ => null
        };
    }

    internal async Task<DiscordBotMessage> ExecutePrevAsync(SocketMessageComponent messageComponent, Deferer deferer, WorldRecordModel wr)
    {
        if (wr.PreviousWorldRecord is null)
        {
            return new DiscordBotMessage(new EmbedBuilder().WithTitle("Out of previous world records.").Build(),
                ephemeral: true, alwaysPostAsNewMessage: true);
        }

        using var scope = _tmwrDiscordBotService.CreateCommand(out WrCommand? wrCommand);

        if (wrCommand is null)
        {
            throw new Exception();
        }

        wrCommand.MapUid = wr.Map.MapUid;
        wrCommand.Guid = wr.PreviousWorldRecord.Guid.ToString();

        var message = await wrCommand.ExecuteAsync(messageComponent, deferer);

        return message with { AlwaysPostAsNewMessage = true, Ephemeral = true };
    }

    internal async Task<DiscordBotMessage> ExecuteComparePrevAsync(SocketMessageComponent messageComponent, Deferer deferer, WorldRecordModel wr)
    {
        if (wr.PreviousWorldRecord is null)
        {
            return new DiscordBotMessage(new EmbedBuilder().WithTitle("No previous world record to compare to.").Build(),
                ephemeral: true, alwaysPostAsNewMessage: true);
        }

        using var scope = _tmwrDiscordBotService.CreateCommand(out CompareCommand.Wrs? compareWrsCommand);

        if (compareWrsCommand is null)
        {
            throw new Exception();
        }

        compareWrsCommand.MapUid = wr.Map.MapUid;
        compareWrsCommand.Guid1 = wr.Guid.ToString();
        compareWrsCommand.Guid2 = wr.PreviousWorldRecord.Guid.ToString();

        var message = await compareWrsCommand.ExecuteAsync(messageComponent, deferer);

        return message with { AlwaysPostAsNewMessage = true, Ephemeral = true };
    }

    internal async Task<DiscordBotMessage> ExecuteNicknameHistoryAsync(SocketMessageComponent messageComponent, Deferer deferer, WorldRecordModel wr)
    {
        using var scope = _tmwrDiscordBotService.CreateCommand(out HistoryCommand.Nickname? nicknameHistoryCommand);

        if (nicknameHistoryCommand is null)
        {
            throw new Exception();
        }

        nicknameHistoryCommand.User = wr.GetPlayerLogin();

        var message = await nicknameHistoryCommand.ExecuteAsync(messageComponent, deferer);

        return message with { AlwaysPostAsNewMessage = true, Ephemeral = true };
    }
}
