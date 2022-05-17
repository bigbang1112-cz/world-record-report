using BigBang1112.DiscordBot.Models;
using Discord;

namespace BigBang1112.WorldRecordReportLib.TMWR;

public abstract class TmwrCommand : DiscordBotCommand
{
    public TmwrCommand(TmwrDiscordBotService tmwrDiscordBotService) : base(tmwrDiscordBotService)
    {
    }

    protected DiscordBotMessage Respond(string? title = null, string? description = null, bool ephemeral = true)
    {
        var embed = new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(description)
            .Build();

        return new DiscordBotMessage(embed, ephemeral: ephemeral);
    }
}
