using BigBang1112.DiscordBot.Models;
using Discord;
using Discord.WebSocket;

namespace BigBang1112.WorldRecordReportLib.TMWR.Commands;

public partial class RecordCountCommand
{
    [DiscordBotSubCommand("campaign", "Shows the amount of records on each map group plus the campaign overall.")]
    [UnfinishedDiscordBotCommand]
    public class Campaign : TmwrCommand
    {
        [DiscordBotCommandOption("historygraph", ApplicationCommandOptionType.Boolean, "Shows the record count \"over time\" graph instead.")]
        public bool HistoryGraph { get; set; }

        [DiscordBotCommandOption("graph", ApplicationCommandOptionType.Boolean, "Shows the record count \"as the campaign progresses\" graph instead.")]
        public bool Graph { get; set; }

        public Campaign(TmwrDiscordBotService tmwrDiscordBotService) : base(tmwrDiscordBotService)
        {

        }

        public override Task<DiscordBotMessage> ExecuteAsync(SocketInteraction slashCommand)
        {
            throw new NotImplementedException();
        }
    }
}
