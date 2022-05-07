using BigBang1112.Extensions;
using BigBang1112.WorldRecordReportLib.Data;
using BigBang1112.WorldRecordReportLib.Models.Db;
using Discord;

namespace BigBang1112.TMWR.Commands;

public partial class MapCommand
{
    [DiscordBotSubCommand("uid", "Gets the UID of the map (MapUid).")]
    public class Uid : MapRelatedCommand
    {
        public Uid(TmwrDiscordBotService tmwrDiscordBotService, IWrUnitOfWork wrUnitOfWork) : base(tmwrDiscordBotService, wrUnitOfWork)
        {

        }

        protected override Task BuildEmbedResponseAsync(MapModel map, EmbedBuilder builder)
        {
            builder.Title = map.MapUid;
            builder.Description = $"{map.GetMdLinkHumanized()} by {map.GetAuthorNicknameMdLink()}";

            return Task.CompletedTask;
        }
    }
}
