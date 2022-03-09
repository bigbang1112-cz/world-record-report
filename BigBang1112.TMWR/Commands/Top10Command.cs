﻿using BigBang1112.Attributes.DiscordBot;
using BigBang1112.WorldRecordReportLib.Models;
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
    public const string OptionSelectableRecords = "selectablerecords";

    private readonly IWrRepo _repo;
    private readonly IRecordSetService _recordSetService;

    public Top10Command(IWrRepo repo, IRecordSetService recordSetService) : base(repo)
    {
        _repo = repo;
        _recordSetService = recordSetService;
    }

    public override IEnumerable<SlashCommandOptionBuilder> YieldOptions()
    {
        foreach (var option in base.YieldOptions())
        {
            yield return option;
        }

        yield return CreateMapUidOption();

        /*yield return new SlashCommandOptionBuilder
        {
            Name = OptionSelectableRecords,
            Type = ApplicationCommandOptionType.Boolean,
            Description = "Adds a select menu for specific record selection."
        };*/
    }

    protected override Task<ComponentBuilder?> CreateComponentsAsync(MapModel map, IEnumerable<SocketSlashCommandDataOption> options)
    {
        /*var recordSet = await _recordSetService.GetFromMapAsync("World", map.MapUid);

        if (recordSet is null)
        {
            return null;
        }

        var selectableRecords = options.Any(x => x.Name == OptionSelectableRecords && (bool)x.Value == true);

        if (!selectableRecords)
        {
            return null;
        }

        var isTMUF = map.Game.IsTMUF();

        var logins = await FetchLoginModelsFromRecordSetAsync(recordSet);

        var selectMenuBuilder = new SelectMenuBuilder()
            .WithCustomId("ok")
            .WithPlaceholder("Select a record...")
            .WithOptions(recordSet.Records.Select((x, i) =>
            {
                var nickname = x.Login;

                if (logins.TryGetValue(x.Login, out LoginModel? loginModel))
                {
                    nickname = loginModel.GetDeformattedNickname();
                }

                return new SelectMenuOptionBuilder(new TimeInt32(x.Time).ToString(useHundredths: isTMUF), i.ToString(), $"by {nickname}");
            }).ToList());

        return new ComponentBuilder()
            .WithSelectMenu(selectMenuBuilder);*/

        return Task.FromResult(default(ComponentBuilder));
    }

    protected override async Task BuildEmbedResponseAsync(MapModel map, EmbedBuilder builder)
    {
        var recordSet = await _recordSetService.GetFromMapAsync("World", map.MapUid);

        var desc = recordSet is null
            ? "No record set found."
            : await CreateTop10DescAsync(recordSet, map.Game.IsTMUF());

        builder.Title = map.GetHumanizedDeformattedName();
        builder.Description = desc;

        var thumbnailUrl = map.GetThumbnailUrl();

        if (thumbnailUrl is not null)
        {
            builder.ThumbnailUrl = thumbnailUrl;
        }
    }

    private async Task<IEnumerable<string>> CreateTop10EnumerableAsync(RecordSet recordSet, bool isTMUF)
    {
        var loginDictionary = await FetchLoginModelsFromRecordSetAsync(recordSet);

        return recordSet.Records.Select(x =>
        {
            var login = x.Login;

            if (loginDictionary.TryGetValue(login, out LoginModel? loginModel))
            {
                login = loginModel.GetDeformattedNickname();
            }

            return $"{x.Rank}. **{new TimeInt32(x.Time).ToString(useHundredths: isTMUF)}** by {login}";
        });
    }

    private async Task<Dictionary<string, LoginModel>> FetchLoginModelsFromRecordSetAsync(RecordSet recordSet)
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

        return loginDictionary;
    }

    private async Task<string> CreateTop10DescAsync(RecordSet recordSet, bool isTMUF)
    {
        return string.Join('\n', await CreateTop10EnumerableAsync(recordSet, isTMUF));
    }
}
