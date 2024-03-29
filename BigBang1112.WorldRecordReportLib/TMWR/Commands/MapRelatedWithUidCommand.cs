﻿using BigBang1112.DiscordBot.Models;
using BigBang1112.WorldRecordReportLib.Data;
using Discord;
using Discord.WebSocket;

namespace BigBang1112.WorldRecordReportLib.TMWR.Commands;

public abstract class MapRelatedWithUidCommand : MapRelatedCommand
{
    private readonly IWrUnitOfWork _wrUnitOfWork;

    protected MapRelatedWithUidCommand(TmwrDiscordBotService tmwrDiscordBotService, IWrUnitOfWork wrUnitOfWork) : base(tmwrDiscordBotService, wrUnitOfWork)
    {
        _wrUnitOfWork = wrUnitOfWork;
    }

    [DiscordBotCommandOption("uid", ApplicationCommandOptionType.String, "UID of the map.")]
    public string? MapUid { get; set; }

    public async Task<IEnumerable<string>> AutocompleteMapUidAsync(string value)
    {
        return await _wrUnitOfWork.Maps.GetAllUidsLikeAsync(value);
    }

    public override async Task<DiscordBotMessage> ExecuteAsync(SocketInteraction slashCommand, Deferer deferer)
    {
        if (MapUid is not null)
        {
            var map = await _wrUnitOfWork.Maps.GetByUidAsync(MapUid);

            if (map is not null)
            {
                return await CreateResponseMessageWithMapsParamAsync(map.Yield(), deferer);
            }
        }

        return await base.ExecuteAsync(slashCommand, deferer);
    }
}
