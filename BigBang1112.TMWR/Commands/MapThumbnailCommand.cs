using BigBang1112.Attributes.DiscordBot;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;
using Discord;

namespace BigBang1112.TMWR.Commands;

[DiscordBotCommand("map thumbnail", "Gets the thumbnail of the map.")]
public class MapThumbnailCommand : MapRelatedCommand
{
    public MapThumbnailCommand(IWrRepo repo) : base(repo)
    {

    }

    public override IEnumerable<SlashCommandOptionBuilder> YieldOptions()
    {
        foreach (var option in base.YieldOptions())
        {
            yield return option;
        }

        yield return CreateMapUidOption();
    }

    protected override Task BuildEmbedResponseAsync(MapModel map, EmbedBuilder builder)
    {
        builder.Title = $"{map.GetHumanizedDeformattedName()} by {map.Author.GetDeformattedNickname()}";
        builder.ImageUrl = map.GetThumbnailUrl();

        return Task.CompletedTask;
    }
}
