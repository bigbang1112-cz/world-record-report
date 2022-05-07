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
}
