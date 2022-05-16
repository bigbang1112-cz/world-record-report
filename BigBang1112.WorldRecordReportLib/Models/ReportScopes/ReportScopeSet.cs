using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using BigBang1112.WorldRecordReportLib.Attributes.ReportScope;

namespace BigBang1112.WorldRecordReportLib.Models.ReportScopes;

public record ReportScopeSet : ReportScope
{
    internal static JsonSerializerOptions JsonSerializerOptions { get; } = new JsonSerializerOptions
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    internal static IEnumerable<string>? cachedAllPossibleReportScopes;

    public ReportScopeTM2020? TM2020 { get; init; }
    public ReportScopeTMUF? TMUF { get; init; }
    public ReportScopeTM2? TM2 { get; init; }

    public static IEnumerable<string> GetAllPossibleReportScopes()
    {
        if (cachedAllPossibleReportScopes is not null)
        {
            return cachedAllPossibleReportScopes;
        }

        cachedAllPossibleReportScopes = GetReportScopes(typeof(ReportScopeSet));

        return cachedAllPossibleReportScopes;
    }

    private static IEnumerable<string> GetReportScopes(Type type, string? currentScopeLayer = null)
    {
        if (type.IsSubclassOf(typeof(ReportScopeWithParam)))
        {
            var atts = type.GetCustomAttributes<ReportScopeParamAttribute>();

            foreach (var att in atts)
            {
                yield return $"{currentScopeLayer}:{att.Value}";
            }
            
            yield break;
        }

        var properties = type.GetProperties();
        
        foreach (var property in properties.Where(x => x.PropertyType.IsSubclassOf(typeof(ReportScope))))
        {
            var scopeName = currentScopeLayer is null
                ? property.Name
                : $"{currentScopeLayer}:{property.Name}";

            yield return scopeName;            

            foreach (var subScope in GetReportScopes(property.PropertyType, scopeName))
            {
                yield return subScope;
            }
        }
    }

    public static IEnumerable<string> GetReportScopesLike(string value, int limit = 25)
    {
        return GetAllPossibleReportScopes()
            .Where(x => x.ToLower().Contains(value))
            .Take(limit);
    }

    public static ReportScopeSet? FromJson(string json)
    {
        return JsonSerializer.Deserialize<ReportScopeSet>(json, JsonSerializerOptions);
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, JsonSerializerOptions);
    }

    public bool TryAdd(string scope, [NotNullWhen(true)] out string? addedScope)
    {
        throw new NotImplementedException();
    }

    public static bool TryParse(
        [NotNullWhen(true)] string? scope,
        [NotNullWhen(true)] out ReportScopeSet? reportScopeSet,
        [NotNullWhen(true)] out string? addedScope)
    {
        if (scope is null)
        {
            reportScopeSet = null;
            addedScope = null;
            return false;
        }

        var scopePath = scope.Split(':');
        var currentProperty = default(PropertyInfo);

        for (var i = 0; i < scopePath.Length; i++)
        {
            var s = scopePath[i];

            if (i == 0)
            {
                currentProperty = typeof(ReportScopeSet).GetProperty(s);
            }
            else if (currentProperty is not null)
            {
                currentProperty = currentProperty.PropertyType.GetProperty(s);
            }
            else
            {
                throw new Exception();
            }
        }

        reportScopeSet = new();

        throw new NotImplementedException();
    }
}
