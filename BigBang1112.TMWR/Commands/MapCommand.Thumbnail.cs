using BigBang1112.Extensions;
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
            var thumbnailUrl = map.GetThumbnailUrl();

            builder.Title = $"{map.GetHumanizedDeformattedName()} by {map.Author.GetDeformattedNickname().EscapeDiscord()}";
            builder.ImageUrl = thumbnailUrl;
            builder.Url = map.GetInfoUrl();

            if (thumbnailUrl is null)
            {
                builder.Description = "No thumbnail found.";
            }

            return Task.CompletedTask;
        }
    }
}
