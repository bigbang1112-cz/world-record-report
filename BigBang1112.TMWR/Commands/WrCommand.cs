using BigBang1112.Extensions;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;
using Discord;

namespace BigBang1112.TMWR.Commands;

[DiscordBotCommand("wr", "Gets the world record of a map.")]
public class WrCommand : MapRelatedWithUidCommand
{
    private readonly TmwrDiscordBotService _tmwrDiscordBotService;
    private readonly IWrRepo _repo;

    [DiscordBotCommandOption("rank",
        ApplicationCommandOptionType.String,
        "GUID of the world record.")]
    public string? Guid { get; set; }

    public async Task<IEnumerable<string>> AutocompleteGuidAsync(string value)
    {
        return await _repo.GetWorldRecordGuidsAsync(value);
    }

    public WrCommand(TmwrDiscordBotService tmwrDiscordBotService, IWrRepo repo) : base(tmwrDiscordBotService, repo)
    {
        _tmwrDiscordBotService = tmwrDiscordBotService;
        _repo = repo;
    }

    public override async Task<DiscordBotMessage> ExecuteAsync(SocketInteraction slashCommand, Deferer deferer)
    {
        if (Guid is null)
        {
            return await base.ExecuteAsync(slashCommand, deferer);
        }

        var wr = await _repo.GetWorldRecordAsync(new Guid(Guid));

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
            ? await _repo.GetWorldRecordAsync(map)
            : await _repo.GetWorldRecordAsync(new Guid(Guid));

        if (wr is null)
        {
            return null;
        }

        return new ComponentBuilder()
            .WithButton("Checkpoints", CreateCustomId($"{wr.Guid.ToString().Replace('-', '_')}-checkpoints"), ButtonStyle.Secondary, disabled: true)
            .WithButton("Inputs", CreateCustomId($"{wr.Guid.ToString().Replace('-', '_')}-inputs"), ButtonStyle.Secondary, disabled: true)
            .WithButton("Previous", CreateCustomId($"{wr.Guid.ToString().Replace('-', '_')}-prev"), ButtonStyle.Secondary)
            .WithButton("Compare with previous", CreateCustomId($"{wr.Guid.ToString().Replace('-', '_')}-compareprev"), ButtonStyle.Secondary);
    }

    protected override async Task BuildEmbedResponseAsync(MapModel map, EmbedBuilder builder)
    {
        var wr = Guid is null
            ? await _repo.GetWorldRecordAsync(map)
            : await _repo.GetWorldRecordAsync(new Guid(Guid));

        var thumbnailUrl = map.GetThumbnailUrl();

        if (thumbnailUrl is not null)
        {
            builder.ThumbnailUrl = thumbnailUrl;
        }

        builder.Title = wr is null
            ? "No world record!"
            : $"{wr.GetTimeFormattedToGame()} by {wr.GetPlayerNicknameDeformatted().EscapeDiscord()}";

        builder.Description = $"{map.GetHumanizedDeformattedName()} by {map.Author.GetDeformattedNickname().EscapeDiscord()}";

        if (wr is null)
        {
            builder.Footer = null;
            return;
        }

        var tmxUrl = map.GetTmxUrl();

        if (tmxUrl is not null)
        {
            builder.Description = $"[{builder.Description}]({tmxUrl})";
        }

        builder.AddField("Driven on", wr.DrivenOn.ToTimestampTag(TimestampTagStyles.LongDateTime));
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
        var wr = await _repo.GetWorldRecordAsync(wrGuid);

        if (wr is null)
        {
            return new DiscordBotMessage(new EmbedBuilder().WithDescription("No world record found.").Build(),
                ephemeral: true, alwaysPostAsNewMessage: true);
        }

        return split[2] switch
        {
            "prev" => await ExecutePrevAsync(messageComponent, deferer, wr),
            "compareprev" => await ExecuteComparePrevAsync(messageComponent, deferer, wr),
            _ => null
        };
    }

    private async Task<DiscordBotMessage> ExecutePrevAsync(SocketMessageComponent messageComponent, Deferer deferer, WorldRecordModel wr)
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

    private async Task<DiscordBotMessage> ExecuteComparePrevAsync(SocketMessageComponent messageComponent, Deferer deferer, WorldRecordModel wr)
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
}
