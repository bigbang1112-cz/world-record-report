namespace BigBang1112.WorldRecordReportLib.TMWR.Commands;

public partial class ReportCommand
{
    [DiscordBotSubCommand("autothread", "Get info about the automatic creation of threads on reports.")]
    public partial class Autothread : TmwrCommand
    {
        public Autothread(TmwrDiscordBotService tmwrDiscordBotService) : base(tmwrDiscordBotService)
        {
        }
    }
}
