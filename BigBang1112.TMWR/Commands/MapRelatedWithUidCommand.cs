using BigBang1112.WorldRecordReportLib.Repos;
using Discord;

namespace BigBang1112.TMWR.Commands;

public abstract class MapRelatedWithUidCommand : MapRelatedCommand
{
    private readonly IWrRepo _repo;

    protected MapRelatedWithUidCommand(TmwrDiscordBotService tmwrDiscordBotService, IWrRepo repo) : base(tmwrDiscordBotService, repo)
    {
        _repo = repo;
    }

    [DiscordBotCommandOption("uid", ApplicationCommandOptionType.String, "UID of the map.")]
    public string? MapUid { get; set; }

    public async Task<IEnumerable<string>> AutocompleteMapUidAsync(string value)
    {
        return await _repo.GetMapUidsAsync(value);
    }

    public override async Task<DiscordBotMessage> ExecuteAsync(SocketInteraction slashCommand)
    {
        if (MapUid is not null)
        {
            var map = await _repo.GetMapByUidAsync(MapUid);

            if (map is not null)
            {
                return await CreateResponseMessageWithMapsParamAsync(Enumerable.Repeat(map, 1));
            }
        }

        return await base.ExecuteAsync(slashCommand);
    }
}