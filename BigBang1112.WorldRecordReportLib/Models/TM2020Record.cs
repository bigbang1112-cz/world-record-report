﻿using TmEssentials;

namespace BigBang1112.WorldRecordReportLib.Models;

public record TM2020Record : IRecord<Guid>
{
    public int Rank { get; init; }
    public Guid PlayerId { get; init; }
    public int Time { get; init; }
    public string? DisplayName { get; init; }
    public string? GhostUrl { get; init; }
    public DateTime Timestamp { get; init; }
    public bool Ignored { get; init; }

    public override string ToString()
    {
        return $"{new TimeInt32(Time)} by {DisplayName ?? PlayerId.ToString()}";
    }
}
