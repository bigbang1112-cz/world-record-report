namespace BigBang1112.TMWR.Commands;

public partial class CheckpointsCommand
{
    [DiscordBotSubCommand("record")]
    [UnfinishedDiscordBotCommand]
    public class Record : DiscordBotCommand
    {
        public Record(TmwrDiscordBotService tmwrDiscordBotService) : base(tmwrDiscordBotService)
        {

        }

        public override Task<DiscordBotMessage> ExecuteAsync(SocketInteraction slashCommand)
        {
            throw new NotImplementedException();
        }
    }
}
