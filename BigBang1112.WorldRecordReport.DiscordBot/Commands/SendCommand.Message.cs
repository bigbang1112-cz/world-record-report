using BigBang1112.DiscordBot;
using BigBang1112.DiscordBot.Attributes;
using BigBang1112.DiscordBot.Models;
using BigBang1112.WorldRecordReportLib.TMWR;
using Discord;
using Discord.WebSocket;

namespace BigBang1112.WorldRecordReport.DiscordBot.Commands;

public partial class SendCommand
{
    [DiscordBotSubCommand("message", "Sends a message to a channel.")]
    public class Message : DiscordBotCommand
    {
        private readonly TmwrDiscordBotService _tmwr;

        [DiscordBotCommandOption("channelid", ApplicationCommandOptionType.String, "Channel ID.", IsRequired = true)]
        public string? ChannelId { get; set; }

        [DiscordBotCommandOption("message", ApplicationCommandOptionType.String, "Message.", IsRequired = true)]
        public string? Msg { get; set; }

        [DiscordBotCommandOption("respondto", ApplicationCommandOptionType.String, "Message ID to respond to.")]
        public string? RespondTo { get; set; }

        public Message(DiscordBotService discordBotService, TmwrDiscordBotService tmwr) : base(discordBotService)
        {
            _tmwr = tmwr;
        }

        public override async Task<DiscordBotMessage> ExecuteAsync(SocketInteraction slashCommand)
        {
            if (!ulong.TryParse(ChannelId, out ulong channelId))
            {
                return new DiscordBotMessage("Missing or wrong channel ID.", ephemeral: true);
            }

            if (string.IsNullOrWhiteSpace(Msg))
            {
                return new DiscordBotMessage("Message is empty.", ephemeral: true);
            }

            var channel = await _tmwr.Client.GetChannelAsync(channelId);

            if (channel is not ITextChannel textChannel)
            {
                return new DiscordBotMessage("Not a text channel.", ephemeral: true);
            }

            var reference = default(MessageReference);

            if (ulong.TryParse(RespondTo, out ulong messageId))
            {
                var msgRespondTo = await textChannel.GetMessageAsync(messageId);

                if (msgRespondTo is not null)
                {
                    reference = new MessageReference(msgRespondTo.Id);
                }
            }
            
            await textChannel.SendMessageAsync(Msg, messageReference: reference);

            return new DiscordBotMessage("Message sent.", ephemeral: true);
        }
    }
}
