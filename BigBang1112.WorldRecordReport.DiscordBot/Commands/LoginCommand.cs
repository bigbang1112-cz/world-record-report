using BigBang1112.DiscordBot;
using BigBang1112.DiscordBot.Attributes;
using BigBang1112.DiscordBot.Models;
using BigBang1112.WorldRecordReportLib.Data;
using Discord;
using Discord.WebSocket;

namespace BigBang1112.WorldRecordReport.DiscordBot.Commands;

[DiscordBotCommand("login", "Get login from nickname.")]
public class LoginCommand : DiscordBotCommand
{
    private readonly WrrDiscordBotService _discordBotService;
    private readonly IWrUnitOfWork _wrUnitOfWork;

    public LoginCommand(WrrDiscordBotService discordBotService, IWrUnitOfWork wrUnitOfWork) : base(discordBotService)
    {
        _discordBotService = discordBotService;
        _wrUnitOfWork = wrUnitOfWork;
    }

    [DiscordBotCommandOption("nickname", ApplicationCommandOptionType.String, "Nickname.", IsRequired = true)]
    public string? Nickname { get; set; }

    internal async Task<IEnumerable<string>> AutocompleteNicknameAsync(string value)
    {
        return await _wrUnitOfWork.Logins.GetAllNicknamesLikeAsync(value, max: 25);
    }

    public override async Task<DiscordBotMessage> ExecuteAsync(SocketInteraction slashCommand)
    {
        var names = await _wrUnitOfWork.Logins.GetNamesByNicknameAsync(Nickname);

        return new()
        {
            Message = names.Any() ? string.Join(", ", names) : "No login found."
        };
    }
}
