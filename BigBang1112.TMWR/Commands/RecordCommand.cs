using BigBang1112.WorldRecordReportLib.Repos;

namespace BigBang1112.TMWR.Commands;

[DiscordBotCommand("record", "Shows information about a certain record.")]
public class RecordCommand : MapRelatedWithUidCommand
{
    [DiscordBotCommandOption("rank",
        Discord.ApplicationCommandOptionType.Integer,
        "Rank of the record in the list.",
        IsRequired = true)]
    public int Rank { get; set; }

    public RecordCommand(TmwrDiscordBotService tmwrDiscordBotService, IWrRepo repo) : base(tmwrDiscordBotService, repo)
    {

    }
}
