using BigBang1112.Attributes.DiscordBot;
using BigBang1112.WorldRecordReportLib.Repos;

namespace BigBang1112.TMWR.Commands;

[DiscordBotCommand("wr history", "Gets the world record history of a map.")]
public class WrHistoryCommand : MapRelatedCommand
{
    public WrHistoryCommand(IWrRepo repo) : base(repo)
    {

    }
}
