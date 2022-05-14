using BigBang1112.WorldRecordReportLib.Data;
using BigBang1112.WorldRecordReportLib.Models.Db;
using Discord;

namespace BigBang1112.WorldRecordReportLib.TMWR.Commands;

public partial class MapCommand
{
    [DiscordBotSubCommand("thumbnail", "Gets the thumbnail of the map.")]
    public class Thumbnail : MapRelatedWithUidCommand
    {
        public Thumbnail(TmwrDiscordBotService tmwrDiscordBotService, IWrUnitOfWork wrUnitOfWork) : base(tmwrDiscordBotService, wrUnitOfWork)
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
