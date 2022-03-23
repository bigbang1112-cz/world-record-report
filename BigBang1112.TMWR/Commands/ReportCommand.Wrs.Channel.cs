using BigBang1112.DiscordBot;
using BigBang1112.DiscordBot.Data;
using Discord;

namespace BigBang1112.TMWR.Commands;

public partial class ReportCommand
{
    public partial class Wrs
    {
        [DiscordBotSubCommand("channel", "Gets the channel information about the subscription, or subscribes to the reports in this channel.")]
        [UnfinishedDiscordBotCommand]
        public class Channel : DiscordBotCommand
        {
            private readonly IDiscordBotRepo _discordBotRepo;

            [DiscordBotCommandOption("set", ApplicationCommandOptionType.Boolean, "If True, world records will be reported in this channel [ManageChannels].")]
            public bool? Set { get; set; }

            [DiscordBotCommandOption("other", ApplicationCommandOptionType.Channel, "Specify other channel to apply/see the subscription to/of.")]
            public SocketChannel? OtherChannel { get; set; }

            public Channel(DiscordBotService discordBotService, IDiscordBotRepo discordBotRepo) : base(discordBotService)
            {
                _discordBotRepo = discordBotRepo;
            }

            public override async Task<DiscordBotMessage> ExecuteAsync(SocketInteraction slashCommand)
            {
                if (OtherChannel is not SocketTextChannel textChannel)
                {
                    if (OtherChannel is not null)
                    {
                        return RespondWithDescriptionEmbed("The specified channel is not a text channel.");
                    }

                    textChannel = slashCommand.Channel as SocketTextChannel ?? throw new Exception();
                }

                if (Set is null)
                {
                    return await GetReportAsync(textChannel)
                        ? RespondWithDescriptionEmbed($"In <#{textChannel.Id}>, world records are **reported**.")
                        : RespondWithDescriptionEmbed($"In <#{textChannel.Id}>, world records are **not** reported.");
                }

                if (slashCommand.User is not SocketGuildUser guildUser)
                {
                    throw new Exception("This user is not a guild user");
                }

                if (!guildUser.GuildPermissions.ManageChannels)
                {
                    return RespondWithDescriptionEmbed($"You don't have permissions to set the report subscription in <#{textChannel.Id}>.");
                }

                try
                {
                    await SetReportAsync(textChannel, Set.Value);

                    return Set.Value
                        ? RespondWithDescriptionEmbed($"In <#{textChannel.Id}>, world records are now reported, **after adding scopes** with `/report wrs scopes add`.")
                        : RespondWithDescriptionEmbed($"In <#{textChannel.Id}>, world records are **no longer reported**.");
                }
                catch (Exception)
                {
                    return RespondWithDescriptionEmbed($"Cannot set world record reporting for <#{textChannel.Id}> due to exception.");
                }
            }

            private async Task<bool> GetReportAsync(SocketTextChannel textChannel)
            {
                var discordBotGuid = GetDiscordBotGuid();

                if (discordBotGuid is null)
                {
                    return false;
                }

                var reportSubscription = await _discordBotRepo.GetReportChannelAsync(discordBotGuid.Value, textChannel);

                return reportSubscription is not null && reportSubscription.Enabled;
            }

            private async Task SetReportAsync(SocketTextChannel textChannel, bool set)
            {
                var discordBotGuid = GetDiscordBotGuid();

                if (discordBotGuid is null)
                {
                    throw new Exception("Missing discord bot guid");
                }

                await _discordBotRepo.AddOrUpdateWorldRecordReportChannelAsync(discordBotGuid.Value, textChannel, set);

                await _discordBotRepo.SaveAsync();
            }

            private static DiscordBotMessage RespondWithDescriptionEmbed(string description)
            {
                var embed = new EmbedBuilder()
                    .WithDescription(description)
                    .Build();

                return new DiscordBotMessage(embed, ephemeral: true);
            }
        }
    }
}
