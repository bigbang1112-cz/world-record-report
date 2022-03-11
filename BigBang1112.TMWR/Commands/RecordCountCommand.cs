using BigBang1112.Attributes.DiscordBot;
using BigBang1112.Models.DiscordBot;
using BigBang1112.Services;
using BigBang1112.WorldRecordReportLib.Repos;
using Discord.WebSocket;

namespace BigBang1112.TMWR.Commands;

[DiscordBotCommand("recordcount")]
public class RecordCountCommand : DiscordBotCommand
{
    public RecordCountCommand(DiscordBotService discordBotService) : base(discordBotService)
    {

    }

    public override Task<DiscordBotMessage> ExecuteAsync(SocketSlashCommand slashCommand)
    {
        throw new NotImplementedException();
    }

    [DiscordBotSubCommand("map", "Shows the amount of records on a map.")]
    public class Map : MapRelatedWithUidCommand
    {
        public Map(TmwrDiscordBotService tmwrDiscordBotService, IWrRepo repo) : base(tmwrDiscordBotService, repo)
        {

        }
    }

    [DiscordBotSubCommand("mapgroup", "Shows the amount of records on each map in map groups plus the map group overall.")]
    public class MapGroup : DiscordBotCommand
    {
        public MapGroup(DiscordBotService discordBotService) : base(discordBotService)
        {

        }

        public override Task<DiscordBotMessage> ExecuteAsync(SocketSlashCommand slashCommand)
        {
            throw new NotImplementedException();
        }
    }
}
