using BigBang1112.DiscordBot;
using Discord;

namespace BigBang1112.TMWR.Commands;

public partial class RecordCountCommand
{
    [DiscordBotSubCommand("mapgroup", "Shows the amount of records on each map in map groups plus the map group overall.")]
    public class MapGroup : DiscordBotCommand
    {
        [DiscordBotCommandOption("historygraph", ApplicationCommandOptionType.Boolean, "Shows the record count \"over time\" graph instead.")]
        public bool HistoryGraph { get; set; }

        [DiscordBotCommandOption("graph", ApplicationCommandOptionType.Boolean, "Shows the record count \"as the map group progresses\" graph instead.")]
        public bool Graph { get; set; }

        public MapGroup(DiscordBotService discordBotService) : base(discordBotService)
        {

        }

        public override Task<DiscordBotMessage> ExecuteAsync(SocketInteraction slashCommand)
        {
            throw new NotImplementedException();
        }
    }
}
