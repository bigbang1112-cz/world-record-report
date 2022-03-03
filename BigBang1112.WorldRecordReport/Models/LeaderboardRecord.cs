using TmEssentials;
using TmXmlRpc;

namespace BigBang1112.WorldRecordReport.Models;

public class LeaderboardRecord
{
    public int Rank { get; init; }
    public TimeInt32 Time { get; init; }
    public string Nickname { get; init; } = default!;
    public string Login { get; init; } = default!;
    public string ReplayUrl { get; init; } = default!;
    public DateTimeOffset Timestamp { get; init; }
    public bool IsFromManialink { get; init; }

    /// <exception cref="HttpRequestException"/>
    public async Task<DateTimeOffset?> GetTimestampAsync()
    {
        var response = await MasterServer.Client.HeadAsync(ReplayUrl);
        response.EnsureSuccessStatusCode();
        return response.Content.Headers.LastModified;
    }
}
