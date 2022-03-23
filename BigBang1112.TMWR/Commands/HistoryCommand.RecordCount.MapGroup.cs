namespace BigBang1112.TMWR.Commands;

public partial class HistoryCommand
{
    public partial class RecordCount
    {
        [DiscordBotSubCommand("mapgroup", "Gets the history of the record count increase in a map group.")]
        [UnfinishedDiscordBotCommand]
        public class MapGroup : DiscordBotCommand
        {
            public MapGroup(TmwrDiscordBotService tmwrDiscordBotService) : base(tmwrDiscordBotService)
            {

            }

            public override Task<DiscordBotMessage> ExecuteAsync(SocketInteraction slashCommand)
            {
                throw new NotImplementedException();
            }
        }
    }
}
