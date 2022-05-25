using BigBang1112.DiscordBot.Models;
using Discord;
using Discord.WebSocket;

namespace BigBang1112.WorldRecordReportLib.TMWR.Commands;

public partial class ReportCommand
{
    [DiscordBotSubCommand("help", "Gives help related to reporting using the bot.")]
    public class Help : TmwrCommand
    {
        public Help(TmwrDiscordBotService tmwrDiscordBotService) : base(tmwrDiscordBotService)
        {
            
        }

        public override Task<DiscordBotMessage> ExecuteAsync(SocketInteraction slashCommand)
        {
            var embed = new EmbedBuilder()
                .WithDescription("You can report all kinds of events in up to 5 Discord channels of your choice per Discord server using the bot's report system. To determine what gets reported, a structural scope system has been made.\n\n" +
                    "`/report scopes explain:[?]` - Receive an explanation about the [?] scope (what you get reported by it).\n\n" +
                    "`/report subscribe scope:[?]` - Subscribe to [?] initial scope using the autocomplete. You can change it anytime later.\n\n" +
                    "**When selecting a parent scope, all of its children scopes will be absorbed with it.** `/report scopes` makes it more clear.\n\n" +
                    "`/report scopes` - Review your scope set. A slightly modified JSON is returned for a cleaner view.\n\n" +
                    "You can add more scopes using the `/report subscribe scope:[?]`, there's no limit on that.\n\n" +
                    "You cannot subscribe to automatically everything (unless you find a way to exploit it). To report everything from all games, you have to subscribe with scope values TMUF, TM2, TM2020, etc separately.\n\n" +
                    "To completely unsubscribe reports from the channel, use `/report unsubscribe`.\n" +
                    "To unsubscribe from a specific scope, use `/report unsubscribe scope:[?]` where [?] is the lowest scope level you want to remove.\n\n" +
                    "**Current design flaw:** If you had a scenario where you would be **only** subscribed to `TMUF:TMX:Official:Changes` and then remove the scope via `/report unsubscribe scope:TMUF:TMX:Official:Changes`, you would get subscribed to `TMUF:TMX:Official`, instead of completely unsubscribing. To completely unsubscribe in this situation without starting over, use `/report unsubscribe scope:TMUF`. Again, you can use `/report scopes` throughout to understand this better.")
                .Build();
            
            return Task.FromResult(new DiscordBotMessage(embed, ephemeral: true));
        }
    }
}
