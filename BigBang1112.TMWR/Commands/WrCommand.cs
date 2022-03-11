﻿using BigBang1112.Attributes.DiscordBot;
using BigBang1112.Extensions;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;
using Discord;

namespace BigBang1112.TMWR.Commands;

[DiscordBotCommand("wr", "Gets the world record of a map.")]
public class WrCommand : MapRelatedWithUidCommand
{
    private readonly IWrRepo _repo;

    public WrCommand(TmwrDiscordBotService tmwrDiscordBotService, IWrRepo repo) : base(tmwrDiscordBotService, repo)
    {
        _repo = repo;
    }

    protected override async Task BuildEmbedResponseAsync(MapModel map, EmbedBuilder builder)
    {
        var wr = await _repo.GetWorldRecordAsync(map);

        var thumbnailUrl = map.GetThumbnailUrl();

        if (thumbnailUrl is not null)
        {
            builder.ThumbnailUrl = thumbnailUrl;
        }

        builder.Title = wr is null
            ? "No world record!"
            : $"{wr.GetTimeFormattedToGame()} by {wr.GetPlayerNicknameDeformatted()}";

        builder.Description = $"{map.GetHumanizedDeformattedName()} by {map.Author.GetDeformattedNickname()}";

        if (wr is null)
        {
            builder.Footer = null;
        }
        else
        { 
            builder.WithBotFooter(wr.Guid.ToString());
        }
    }
}
