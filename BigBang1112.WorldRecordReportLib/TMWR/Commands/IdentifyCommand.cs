using System.Text;
using BigBang1112.DiscordBot.Models;
using BigBang1112.WorldRecordReportLib.TMWR.Extensions;
using Discord;
using Discord.WebSocket;

namespace BigBang1112.WorldRecordReportLib.TMWR.Commands;

[DiscordBotCommand("identify", "Identify a user from their login or nickname.")]
public class IdentifyCommand : IdentifyBaseCommand
{
    private readonly IWrUnitOfWork _wrUnitOfWork;

    public IdentifyCommand(TmwrDiscordBotService tmwrDiscordBotService, IWrUnitOfWork wrUnitOfWork) : base(tmwrDiscordBotService)
    {
        _wrUnitOfWork = wrUnitOfWork;
    }

    public override async Task<DiscordBotMessage> ExecuteAsync(SocketInteraction slashCommand)
    {
        if (User is null)
        {
            return new DiscordBotMessage(new EmbedBuilder().WithDescription("User login or nickname not specified.").Build(), ephemeral: true);
        }

        var logins = await _wrUnitOfWork.Logins.GetByNameAsync(User);
        var nicknames = await _wrUnitOfWork.Logins.GetByNicknameAsync(User);

        foreach (var (game, login) in logins)
        {
            if (!nicknames.ContainsKey(game))
            {
                nicknames[game] = new() { login };
                continue;
            }

            var list = nicknames[game];

            if (!list.Any(x => x.Name == login.Name))
            {
                list.Add(login);
            }
        }

        var sb = new StringBuilder();

        if (nicknames.Count == 0)
        {
            sb.Append("No user found with this login/nickname.");
        }
        else
        {
            foreach (var (game, list) in nicknames)
            {
                sb.AppendLine($"**{game}**");

                foreach (var login in list)
                {
                    sb.AppendLine($"- {login.GetDeformattedNickname()} (**{login.Name}**)");
                }
            }
        }

        var embed = new EmbedBuilder()
            .WithTitle($"User(s) \"{User}\"")
            .WithDescription(sb.ToString())
            .WithBotFooter("Identify")
            .Build();

        return new DiscordBotMessage(embed);
    }
}
