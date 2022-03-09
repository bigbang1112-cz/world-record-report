using BigBang1112.Attributes.DiscordBot;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;
using Discord;

namespace BigBang1112.TMWR.Commands;

[DiscordBotCommand("map uid", "Gets the UID of the map (MapUid).")]
public class MapUidCommand : MapRelatedCommand
{
    public MapUidCommand(IWrRepo repo) : base(repo)
    {

    }

    protected override Task<Embed> CreateEmbedResponseAsync(MapModel map)
    {
        var embed = new EmbedBuilder()
           .WithTitle(map.MapUid)
           .WithDescription($"{map.GetHumanizedDeformattedName()} by {map.Author.GetDeformattedNickname()}")
           .Build();

        return Task.FromResult(embed);
    }
}
