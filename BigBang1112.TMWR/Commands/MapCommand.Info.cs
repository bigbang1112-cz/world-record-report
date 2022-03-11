using BigBang1112.Attributes.DiscordBot;
using BigBang1112.Models.DiscordBot;
using BigBang1112.WorldRecordReportLib.Repos;
using Discord.WebSocket;

namespace BigBang1112.TMWR.Commands;

public partial class MapCommand
{
    [DiscordBotSubCommand("info", "Gets information about the map.")]
    public class Info : MapRelatedWithUidCommand
    {
        public Info(TmwrDiscordBotService tmwrDiscordBotService, IWrRepo repo) : base(tmwrDiscordBotService, repo)
        {

        }
    }
}
