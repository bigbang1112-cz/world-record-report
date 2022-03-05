namespace BigBang1112.WorldRecordReportLib.Models;

public class LbManialinkReport
{
	public string MapName { get; init; } = default!;
	public string MapUid { get; init; } = default!;
	public string Campaign { get; init; } = default!;
	public int Time { get; init; }
	public string Login { get; init; } = default!;
	public string Nickname { get; init; } = default!;
	public int Timestamp { get; init; }
	public string ReplayUrl { get; init; } = default!;
	public int? FormerTime { get; set; }
	public string? FormerLogin { get; set; } = default!;
	public string? FormerNickname { get; set; } = default!;
	public int? FormerTimestamp { get; set; }
	public string FormerReplayUrl { get; init; } = default!;
}
