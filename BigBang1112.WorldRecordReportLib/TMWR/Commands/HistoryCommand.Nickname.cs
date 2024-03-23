using System.Text;
using BigBang1112.DiscordBot.Models;
using BigBang1112.WorldRecordReportLib.TMWR.Extensions;
using Discord;
using Discord.WebSocket;
using TmEssentials;

namespace BigBang1112.WorldRecordReportLib.TMWR.Commands;

public partial class HistoryCommand
{
    [DiscordBotSubCommand("nickname")]
    public class Nickname : IdentifyBaseCommand
    {
        private readonly IWrUnitOfWork _wrUnitOfWork;

        [DiscordBotCommandOption("game", ApplicationCommandOptionType.String, "Game of the login/nickname.", IsRequired = true)]
        public string? Game { get; set; }

        internal async Task<IEnumerable<string>> AutocompleteGameAsync(string value)
        {
            return await _wrUnitOfWork.Games.GetAllNamesLikeAsync(value, max: 25);
        }

        public Nickname(TmwrDiscordBotService tmwrDiscordBotService, IWrUnitOfWork wrUnitOfWork) : base(tmwrDiscordBotService)
        {
            _wrUnitOfWork = wrUnitOfWork;
        }

        public override async Task<DiscordBotMessage> ExecuteAsync(SocketInteraction slashCommand)
        {
            if (User is null || Game is null)
            {
                throw new Exception();
            }

            var gameModel = await _wrUnitOfWork.Games.GetByNameAsync(Game);

            if (gameModel is null)
            {
                return new DiscordBotMessage { Message = $"{Game} not found." };
            }

            var login = await _wrUnitOfWork.Logins.GetByNameAsync(gameModel, User);

            login ??= await _wrUnitOfWork.Logins.GetByNicknameAsync(gameModel, User);

            if (login is null)
            {
                return new DiscordBotMessage(new EmbedBuilder()
                    .WithDescription("User not found.")
                    .WithBotFooter("Nickname history")
                    .Build());
            }

            var history = await _wrUnitOfWork.NicknameChanges.GetHistoryAsync(login);

            var sb = new StringBuilder($"*Note: Because of a bug found on {new DateTime(2024, 3, 23, 17, 0, 0).ToTimestampTag(TimestampTagStyles.ShortDate)}, timestamps could sometimes be innacurate.*\n\n");

            var latestNickname = login.GetDeformattedNickname();

            sb.Append(string.IsNullOrWhiteSpace(latestNickname) ? "*(empty)*" : $"**{latestNickname}**");

            foreach (var change in history)
            {
                sb.AppendLine($" ({change.PreviousLastSeenOn.ToTimestampTag(TimestampTagStyles.ShortDate)})");

                var nickname = TextFormatter.Deformat(change.Previous);
                sb.Append(string.IsNullOrWhiteSpace(nickname) ? "*(empty)*" : $"**{nickname}**");
            }

            var embed = new EmbedBuilder()
                .WithTitle($"Nickname history of \"{User}\"")
                .WithDescription(sb.ToString())
                .WithBotFooter("Nickname history")
                .Build();

            return new DiscordBotMessage(embed);
        }
    }
}
