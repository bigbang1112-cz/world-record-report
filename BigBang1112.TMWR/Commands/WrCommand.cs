using BigBang1112.Attributes.DiscordBot;
using BigBang1112.WorldRecordReportLib.Repos;

namespace BigBang1112.TMWR.Commands;

[DiscordBotCommand("wr", "Gets the world record of a map.")]
public class WrCommand : MapRelatedCommand
{
    public WrCommand(IWrRepo repo) : base(repo)
    {

    }
}
