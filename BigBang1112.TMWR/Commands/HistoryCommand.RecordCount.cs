namespace BigBang1112.TMWR.Commands;

public partial class HistoryCommand
{
    [DiscordBotSubCommand("recordcount")]
    [UnfinishedDiscordBotCommand]
    public partial class RecordCount : DiscordBotCommand
    {
        public RecordCount(TmwrDiscordBotService tmwrDiscordBotService) : base(tmwrDiscordBotService)
        {

        }
    }
}
