using BigBang1112.Attributes.DiscordBot;
using BigBang1112.WorldRecordReportLib.Repos;

namespace BigBang1112.TMWR.Commands;

[DiscordBotCommand("mapuid", "Gets the UID of the map (MapUid).")]
public class MapuidCommand : MapRelatedCommand
{
    public MapuidCommand(IWrRepo repo) : base(repo)
    {

    }
}
