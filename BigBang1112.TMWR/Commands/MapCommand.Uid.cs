using BigBang1112.Extensions;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;
using Discord;

namespace BigBang1112.TMWR.Commands;

public partial class MapCommand
{
    [DiscordBotSubCommand("uid", "Gets the UID of the map (MapUid).")]
    public class Uid : MapRelatedCommand
    {
        public Uid(TmwrDiscordBotService tmwrDiscordBotService, IWrRepo repo) : base(tmwrDiscordBotService, repo)
        {

        }

        protected override Task BuildEmbedResponseAsync(MapModel map, EmbedBuilder builder)
        {
            builder.Title = map.MapUid;
            builder.Description = $"{map.GetHumanizedDeformattedName()} by {map.Author.GetDeformattedNickname().EscapeDiscord()}";
            
            var tmxUrl = map.GetTmxUrl();

            if (tmxUrl is not null)
            {
                builder.Description = $"[{builder.Description}]({tmxUrl})";
            }

            return Task.CompletedTask;
        }
    }
}
