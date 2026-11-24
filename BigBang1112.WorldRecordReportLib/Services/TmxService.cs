using ManiaAPI.TMX;

using GBX.NET;
using GBX.NET.Engines.Game;

namespace BigBang1112.WorldRecordReportLib.Services;

public class TmxService : ITmxService
{
    private readonly IHttpClientFactory _httpFactory;

    public TmxService(IHttpClientFactory httpFactory)
    {
        _httpFactory = httpFactory;
    }

    public async Task<ItemCollection<ReplayItem>> GetReplaysAsync(TmxSite site, long tmxId, CancellationToken cancellationToken = default)
    {
        var http = _httpFactory.CreateClient("resilient");
        var tmx = new TMX(http, site);
        return await tmx.GetReplaysAsync(new() { TrackId = tmxId, Best = true }, cancellationToken);
    }

    public IEnumerable<ReplayItem> GetWrHistory(DateTimeOffset trackAt, ItemCollection<ReplayItem> replays, bool isStunts = false, bool rankedOnly = false)
    {
        var tempTime = default(int?);

        var validReplays = rankedOnly
            ? replays.Results.Where(x => x.Position.HasValue)
            : replays.Results;

        foreach (var replay in validReplays.OrderBy(x => x.ReplayAt))
        {
            if (tempTime is null)
            {
                tempTime = isStunts ? replay.ReplayScore : replay.ReplayTime.TotalMilliseconds;
                yield return replay;
            }

            if (isStunts && replay.ReplayScore > tempTime)
            {
                tempTime = replay.ReplayScore;
                yield return replay;
            }

            if (!isStunts && replay.ReplayTime.TotalMilliseconds < tempTime)
            {
                tempTime = replay.ReplayTime.TotalMilliseconds;
                yield return replay;
            }
        }
    }

    public async Task<ItemCollection<TrackItem>> SearchAsync(TmxSite site, TMX.SearchTracksParameters trackSearchFilters, CancellationToken cancellationToken = default)
    {
        var http = _httpFactory.CreateClient("resilient");
        var tmx = new TMX(http, site);
        return await tmx.SearchTracksAsync(trackSearchFilters, cancellationToken);
    }

    public async Task<string> GetTrackAuthorLoginAsync(TmxSite site, long trackId, CancellationToken cancellationToken = default)
    {
        var tmx = new TMX(_httpFactory.CreateClient("resilient"), site);
        using var response = await tmx.GetTrackGbxResponseAsync(trackId, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var map = Gbx.ParseHeaderNode<CGameCtnChallenge>(stream);
        return !string.IsNullOrWhiteSpace(map.AuthorLogin)
            ? map.AuthorLogin
            : throw new InvalidDataException($"TMX track {trackId} has no author login in its GBX header.");
    }
}
