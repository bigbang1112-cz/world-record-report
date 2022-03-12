using BigBang1112.Attributes.DiscordBot;
using BigBang1112.Extensions;
using BigBang1112.Models.DiscordBot;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;
using Discord;
using Discord.WebSocket;

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
            builder.Description = $"{map.GetHumanizedDeformattedName()} by {map.Author.GetDeformattedNickname()}";

            return Task.CompletedTask;
        }
    }
}
