using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;
using Discord;

namespace BigBang1112.TMWR.Commands;

public partial class MapCommand
{
    [DiscordBotSubCommand("thumbnail", "Gets the thumbnail of the map.")]
    public class Thumbnail : MapRelatedWithUidCommand
    {
        public Thumbnail(TmwrDiscordBotService tmwrDiscordBotService, IWrRepo repo) : base(tmwrDiscordBotService, repo)
        {

        }

        protected override Task BuildEmbedResponseAsync(MapModel map, EmbedBuilder builder)
        {
            builder.Title = $"{map.GetHumanizedDeformattedName()} by {map.Author.GetDeformattedNickname()}";
            builder.ImageUrl = map.GetThumbnailUrl();
            builder.Url = map.GetTmxUrl();

            return Task.CompletedTask;
        }
    }
}
