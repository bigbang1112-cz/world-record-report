namespace BigBang1112.WorldRecordReport.Models;

public class LbManialinkMember
{
	public string Login { get; init; } = default!;
	public string Nickname { get; init; } = default!;
	public string Zone { get; init; } = default!;
	public string Joined { get; init; } = default!;
	public string LastVisited { get; init; } = default!;
	public int Visits { get; init; }
	public bool IsIWRUP { get; init; }
}
