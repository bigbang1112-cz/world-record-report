using BigBang1112.DiscordBot.Attributes;
using BigBang1112.DiscordBot.Models;
using BigBang1112.WorldRecordReportLib.Data;
using BigBang1112.WorldRecordReportLib.Models;
using BigBang1112.WorldRecordReportLib.Models.Db;
using BigBang1112.WorldRecordReportLib.Services;
using Discord;
using Discord.WebSocket;
using Mapster;

namespace BigBang1112.WorldRecordReport.DiscordBot.Commands;

[DiscordBotCommand("ignorelogin", "Ignores a login from reports, usually due to cheating.")]
public class IgnoreLoginCommand : DiscordBotCommand
{
    private readonly WrrDiscordBotService _discordBotService;
    private readonly IWrUnitOfWork _wrUnitOfWork;
    private readonly RecordStorageService _recordStorageService;
    private readonly RefreshTM2020Service _refreshTM2020Service;

    public IgnoreLoginCommand(WrrDiscordBotService discordBotService, IWrUnitOfWork wrUnitOfWork, RecordStorageService recordStorageService, RefreshTM2020Service refreshTM2020Service) : base(discordBotService)
    {
        _discordBotService = discordBotService;
        _wrUnitOfWork = wrUnitOfWork;
        _recordStorageService = recordStorageService;
        _refreshTM2020Service = refreshTM2020Service;
    }

    [DiscordBotCommandOption("game", ApplicationCommandOptionType.String, "Game of the login.", IsRequired = true)]
    public string? Game { get; set; }

    public async Task<IEnumerable<string>> AutocompleteGameAsync(string value)
    {
        return await _wrUnitOfWork.Games.GetAllNamesLikeAsync(value, max: 25);
    }

    [DiscordBotCommandOption("name", ApplicationCommandOptionType.String, "Login name.", IsRequired = true)]
    public string? Name { get; set; }

    public async Task<IEnumerable<string>> AutocompleteNameAsync(string value)
    {
        return await _wrUnitOfWork.Logins.GetAllNamesLikeAsync(value, max: 25);
    }

    public override async Task<DiscordBotMessage> ExecuteAsync(SocketInteraction slashCommand, Deferer deferer)
    {
        if (Game is null || Name is null)
        {
            throw new Exception();
        }

        var allIgnoredLogins = (await _wrUnitOfWork.IgnoredLogins.GetAllAsync())
            .Select(x => x.Login.Id);

        var gameModel = await _wrUnitOfWork.Games.GetByNameAsync(Game);

        if (gameModel is null)
        {
            return new DiscordBotMessage { Message = $"{Game} not found." };
        }

        var loginModel = await _wrUnitOfWork.Logins.GetByGameAndNameAsync(gameModel, Name);

        if (loginModel is null)
        {
            return new DiscordBotMessage { Message = $"{Name} not found." };
        }

        if (allIgnoredLogins.Contains(loginModel.Id))
        {
            return new DiscordBotMessage { Message = $"{loginModel.GetDeformattedNickname()} is already ignored." };
        }

        await deferer.DeferAsync();

        _wrUnitOfWork.IgnoredLogins.Add(new IgnoredLoginModel
        {
            Login = loginModel,
            IgnoredOn = DateTime.UtcNow
        });

        await _wrUnitOfWork.SaveAsync();

        foreach (var map in await _wrUnitOfWork.WorldRecords.GetAllMapsOfPlayerAsync(loginModel))
        {
            await _refreshTM2020Service.RefreshAsync(map, forceUpdate: true);
        }

        return new DiscordBotMessage { Message = $"{loginModel.GetDeformattedNickname()} will eventually disappear." };
    }
}
