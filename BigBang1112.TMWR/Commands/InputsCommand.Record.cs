﻿namespace BigBang1112.TMWR.Commands;

public partial class InputsCommand
{
    [DiscordBotSubCommand("record")]
    public class Record : DiscordBotCommand
    {
        public Record(TmwrDiscordBotService tmwrDiscordBotService) : base(tmwrDiscordBotService)
        {

        }

        public override Task<DiscordBotMessage> ExecuteAsync(SocketSlashCommand slashCommand)
        {
            throw new NotImplementedException();
        }
    }
}
