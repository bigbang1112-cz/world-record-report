using BigBang1112.Extensions;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;
using Discord;

namespace BigBang1112.TMWR.Commands;

[DiscordBotCommand("wr", "Gets the world record of a map.")]
public class WrCommand : MapRelatedWithUidCommand
{
    private readonly IWrRepo _repo;

    public WrCommand(TmwrDiscordBotService tmwrDiscordBotService, IWrRepo repo) : base(tmwrDiscordBotService, repo)
    {
        _repo = repo;
    }

    protected override async Task<ComponentBuilder?> CreateComponentsAsync(MapModel map, bool isModified)
    {
        var wr = await _repo.GetWorldRecordAsync(map);

        if (wr is null)
        {
            return null;
        }

        return new ComponentBuilder()
            .WithButton("Checkpoints", CreateCustomId($"{wr.Guid}-checkpoints"), ButtonStyle.Secondary, disabled: true)
            .WithButton("Inputs", CreateCustomId($"{wr.Guid}-inputs"), ButtonStyle.Secondary, disabled: true)
            .WithButton("Compare with previous", CreateCustomId($"{wr.Guid}-compareprev"), ButtonStyle.Secondary, disabled: true);
    }

    protected override async Task BuildEmbedResponseAsync(MapModel map, EmbedBuilder builder)
    {
        var wr = await _repo.GetWorldRecordAsync(map);

        var thumbnailUrl = map.GetThumbnailUrl();

        if (thumbnailUrl is not null)
        {
            builder.ThumbnailUrl = thumbnailUrl;
        }

        builder.Title = wr is null
            ? "No world record!"
            : $"{wr.GetTimeFormattedToGame()} by {wr.GetPlayerNicknameDeformatted()}";

        builder.Description = $"{map.GetHumanizedDeformattedName()} by {map.Author.GetDeformattedNickname()}";

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
        builder.Timestamp = DateTime.SpecifyKind(wr.DrivenOn, DateTimeKind.Utc);
        builder.WithBotFooter(wr.Guid.ToString());
    }
}
