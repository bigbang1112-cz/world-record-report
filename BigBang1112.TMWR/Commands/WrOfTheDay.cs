using BigBang1112.Attributes.DiscordBot;
using BigBang1112.Models.DiscordBot;
using BigBang1112.Services;
using BigBang1112.TMWR;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[DiscordBotCommand("wroftheday")]
public class WrOfTheDayCommand : DiscordBotCommand
{
    public WrOfTheDayCommand(TmwrDiscordBotService tmwrDiscordBotService) : base(tmwrDiscordBotService)
    {

    }

    public override Task<DiscordBotMessage> ExecuteAsync(SocketSlashCommand slashCommand)
    {
        throw new NotImplementedException();
    }
}
