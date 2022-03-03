﻿namespace BigBang1112.WorldRecordReport.Models;

public record WorldRecordMessageFormat(string? Message, WorldRecordEmbedFormat? Embed)
{
    public readonly WorldRecordMessageFormat Default = new(
        Message: null,
        Embed: new WorldRecordEmbedFormat());
}