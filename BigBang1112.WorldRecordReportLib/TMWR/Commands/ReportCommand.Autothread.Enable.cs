using BigBang1112.DiscordBot.Data;
using BigBang1112.DiscordBot.Models;
using BigBang1112.DiscordBot.Models.Db;
using Discord;
using Discord.WebSocket;

namespace BigBang1112.WorldRecordReportLib.TMWR.Commands;

public partial class ReportCommand
{
    public partial class Autothread
    {
        [DiscordBotSubCommand("enable", "Enables the automatic creation of threads on a report.")]
        public class Enable : TmwrCommand
        {
            private readonly IDiscordBotUnitOfWork _discordBotUnitOfWork;

            [DiscordBotCommandOption("archiveafter", ApplicationCommandOptionType.Integer, "Tells after how much time the thread should be archived by default. Default: One day",
                Choices = new[] { "One hour", "One day", "Three days", "One week" })]
            public long ArchiveAfter { get; set; } = 1;
            
            [DiscordBotCommandOption("other", ApplicationCommandOptionType.Channel, "Specify other channel to apply/see the auto-threading to/of.")]
            public SocketChannel? OtherChannel { get; set; }

            public Enable(TmwrDiscordBotService tmwrDiscordBotService,
                          IDiscordBotUnitOfWork discordBotUnitOfWork) : base(tmwrDiscordBotService)
            {
                _discordBotUnitOfWork = discordBotUnitOfWork;
            }

            public override async Task<DiscordBotMessage> ExecuteAsync(SocketInteraction slashCommand)
            {
                if (OtherChannel is not SocketTextChannel textChannel)
                {
                    if (OtherChannel is not null)
                    {
                        return Respond(description: "The specified channel is not a text channel.");
                    }

                    if (slashCommand.Channel is not SocketTextChannel guildTextChannel)
                    {
                        return Respond(description: "You cannot report to your DMs.");
                    }

                    textChannel = guildTextChannel;
                }

                var reportChannel = await GetReportChannelAsync(textChannel);

                if (reportChannel is null)
                {
                    return Respond(description: $"No reports are subscribed in {textChannel.Mention}.");
                }

                var archiveDuration = ArchiveAfter switch
                {
                    0 => ThreadArchiveDuration.OneHour,
                    1 => ThreadArchiveDuration.OneDay,
                    2 => ThreadArchiveDuration.ThreeDays,
                    3 => ThreadArchiveDuration.OneWeek,
                    _ => ThreadArchiveDuration.OneDay
                };

                reportChannel.ThreadOptions = new AutoThreadOptions(archiveDuration);

                await _discordBotUnitOfWork.SaveAsync();

                return Respond(description: $"Enabled automatic creation of threads in {textChannel.Mention}, automatically archived after `{archiveDuration}`.", ephemeral: false);
            }

            private async Task<ReportChannelModel?> GetReportChannelAsync(SocketTextChannel textChannel)
            {
                var discordBotGuid = GetDiscordBotGuid();

                if (discordBotGuid is null)
                {
                    return null;
                }

                return await _discordBotUnitOfWork.ReportChannels.GetByBotAndTextChannelAsync(discordBotGuid.Value, textChannel);
            }
        }
    }
}
