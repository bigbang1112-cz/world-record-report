﻿using BigBang1112.DiscordBot;
using BigBang1112.DiscordBot.Data;
using BigBang1112.WorldRecordReportLib.Models.ReportScopes;
using Discord;
using Discord.WebSocket;

namespace BigBang1112.WorldRecordReportLib.TMWR.Commands;

public partial class ReportCommand
{
    [DiscordBotSubCommand("unsubscribe", "Gets the channel information about the subscription, or subscribes to the reports in this channel.")]
    public class Unsubscribe : TmwrCommand
    {
        private readonly IDiscordBotUnitOfWork _discordBotUnitOfWork;

        [DiscordBotCommandOption("scope", ApplicationCommandOptionType.String, "Scope to unsubscribe. You can also use /report scopes remove")]
        public bool? Scope { get; set; }

        internal static IEnumerable<string> AutocompleteScope(string value)
        {
            return ReportScopeSet.GetReportScopesLike(value);
        }

        [DiscordBotCommandOption("other", ApplicationCommandOptionType.Channel, "Specify other channel to apply/see the subscription to/of.")]
        public SocketChannel? OtherChannel { get; set; }

        public Unsubscribe(TmwrDiscordBotService tmwrDiscordBotService, IDiscordBotUnitOfWork discordBotUnitOfWork) : base(tmwrDiscordBotService)
        {
            _discordBotUnitOfWork = discordBotUnitOfWork;
        }
    }
}