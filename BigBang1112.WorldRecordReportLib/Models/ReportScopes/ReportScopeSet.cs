﻿using System.Text.Json;
using System.Text.Json.Serialization;

namespace BigBang1112.WorldRecordReportLib.Models.ReportScopes;

public record ReportScopeSet : ReportScope
{
    internal static JsonSerializerOptions JsonSerializerOptions { get; } = new JsonSerializerOptions
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public ReportScopeTM2020? TM2020 { get; init; }
    public ReportScopeTmx? TMX { get; init; }
    public ReportScopeTM2? TM2 { get; init; }
}
