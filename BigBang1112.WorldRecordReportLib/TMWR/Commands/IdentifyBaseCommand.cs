using Discord;

namespace BigBang1112.WorldRecordReportLib.TMWR.Commands;

public abstract class IdentifyBaseCommand : TmwrCommand
{
    [DiscordBotCommandOption("user", ApplicationCommandOptionType.String, "Login or nickname of the user.", IsRequired = true)]
    public string? User { get; set; }

    protected IdentifyBaseCommand(TmwrDiscordBotService tmwrDiscordBotService) : base(tmwrDiscordBotService)
    {

    }
}
