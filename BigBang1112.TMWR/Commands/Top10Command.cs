﻿using BigBang1112.Attributes.DiscordBot;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Repos;
using BigBang1112.WorldRecordReportLib.Services;
using Discord;
using Discord.WebSocket;
using TmEssentials;

namespace BigBang1112.TMWR.Commands;

[DiscordBotCommand("top10", "Shows the Top 10 world leaderboard.")]
public class Top10Command : MapRelatedCommand
{
    private readonly IWrRepo _repo;
    private readonly IRecordSetService _recordSetService;

    public Top10Command(IWrRepo repo, IRecordSetService recordSetService) : base(repo)
    {
        _repo = repo;
        _recordSetService = recordSetService;
    }

    protected override async Task<Embed> CreateEmbedAsync(MapModel map, bool hasMultipleSameNames)
    {
        var recordSet = await _recordSetService.GetFromMapAsync("World", map.MapUid);

        var desc = "";

        if (recordSet is null)
        {
            desc = "No record set found.";
        }
        else
        {
            var loginDictionary = new Dictionary<string, LoginModel>();

            foreach (var login in recordSet.Records.Select(x => x.Login))
            {
                var loginModel = await _repo.GetLoginAsync(login);

                if (loginModel is null)
                {
                    continue;
                }

                loginDictionary[login] = loginModel;
            }

            desc = string.Join('\n', recordSet.Records.Select(x =>
            {
                var login = x.Login;

                if (loginDictionary.TryGetValue(login, out LoginModel? loginModel))
                {
                    login = loginModel.GetDeformattedNickname();
                }

                return $"{x.Rank}. **{new TimeInt32(x.Time)}** by {login}";
            }));
        }

        return new EmbedBuilder()
            .WithTitle(hasMultipleSameNames ? $"{map.GetHumanizedDeformattedName()}" : map.DeformattedName)
            .WithDescription(desc)
            .Build();
    }
}
