using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using BigBang1112.WorldRecordReportLib.Attributes.ReportScope;

namespace BigBang1112.WorldRecordReportLib.Models.ReportScopes;

public sealed record ReportScopeSet : ReportScope
{
    private const BindingFlags BindingAttr = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;

    internal static JsonSerializerOptions JsonSerializerOptions { get; } = new JsonSerializerOptions
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    internal static IEnumerable<string>? cachedAllPossibleReportScopes;

    [ReportScopeExplanation("Reports every tracked event in TM2020")]
    public ReportScopeTM2020? TM2020 { get; init; }

    [ReportScopeExplanation("Reports every tracked event in TMUF")]
    public ReportScopeTMUF? TMUF { get; init; }

    [ReportScopeExplanation("Reports every tracked event in TM2")]
    public ReportScopeTM2? TM2 { get; init; }
    
    [ReportScopeExplanation("Reports everything new about the bot")]
    public ReportScopeNews? News { get; init; }

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
            .Where(x => x.Contains(value, StringComparison.OrdinalIgnoreCase))
            .Take(limit);
    }

    public static ReportScopeSet? FromJson(string json)
    {
        return JsonSerializer.Deserialize<ReportScopeSet>(json, JsonSerializerOptions);
    }

    public string ToJson(bool enableFormatting = false)
    {
        var options = enableFormatting ?
            new JsonSerializerOptions(JsonSerializerOptions) { WriteIndented = enableFormatting }
            : JsonSerializerOptions;

        return JsonSerializer.Serialize(this, options);
    }

    public IEnumerable<string> GetReportScopes(int limit = 25)
    {
        throw new NotImplementedException();
    }

    public bool TryAdd(string scope, [NotNullWhen(true)] out string? addedScope)
    {
        _ = scope ?? throw new ArgumentNullException(nameof(scope));

        var scopePath = scope.Split(':');
        var exactScopeList = new List<string>();

        var currentProperty = default(PropertyInfo);
        var currentValue = (ReportScope)this;
        var startingObject = default(ReportScope);
        var startingObjectOwner = default(ReportScope);
        var startingObjectProp = default(PropertyInfo);

        for (var i = 0; i < scopePath.Length; i++)
        {
            var s = scopePath[i];

            if (i == 0)
            {
                currentProperty = typeof(ReportScopeSet).GetProperty(s, BindingAttr);
            }
            else if (currentProperty is not null)
            {
                currentProperty = currentProperty.PropertyType.GetProperty(s, BindingAttr);
            }
            else
            {
                throw new Exception($"Index {i} has null property.");
            }

            if (currentProperty is null)
            {
                if (currentValue is null)
                {
                    addedScope = null;
                    return false;
                }

                var type = currentValue.GetType();

                if (!type.IsSubclassOf(typeof(ReportScopeWithParam)))
                {
                    addedScope = null;
                    return false;
                }
                
                // verify casing with attribute name
                exactScopeList.Add(s);
                addedScope = string.Join(':', exactScopeList);

                var paramProp = type.GetProperty(nameof(ReportScopeWithParam.Param)) ?? throw new Exception("Param couldn't be found");
                
                if (paramProp.GetValue(currentValue) is string[] param)
                {
                    if (param.Contains(s, StringComparer.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                    
                    // verify casing with attribute name
                    paramProp.SetValue(currentValue, param.Append(s).ToArray());
                }
                else
                {
                    // verify casing with attribute name
                    paramProp.SetValue(currentValue, new string[] { s });
                }
                
                startingObjectProp?.SetValue(startingObjectOwner, startingObject);

                return true;
            }

            if (startingObject is null)
            {
                var temp = currentValue;
                currentValue = currentProperty.GetValue(currentValue) as ReportScope;

                if (currentValue is null)
                {
                    startingObject = Activator.CreateInstance(currentProperty.PropertyType) as ReportScope;
                    startingObjectOwner = temp;
                    startingObjectProp = currentProperty;

                    currentValue = startingObject;
                }
            }
            else
            {
                var val = Activator.CreateInstance(currentProperty.PropertyType);
                currentProperty.SetValue(currentValue, val);
                currentValue = val as ReportScope;
            }

            exactScopeList.Add(currentProperty.Name);
        }

        addedScope = string.Join(':', exactScopeList);

        if (startingObject is null || startingObjectOwner is null || startingObjectProp is null)
        {
            return false;
        }

        startingObjectProp.SetValue(startingObjectOwner, startingObject); 

        return true;
    }

    public bool TryRemove(string scope, [NotNullWhen(true)] out string? removedScope)
    {
        _ = scope ?? throw new ArgumentNullException(nameof(scope));

        var scopePath = scope.Split(':');

        var exactScopeList = new List<string>();

        var currentProperty = default(PropertyInfo);
        var currentValue = (ReportScope)this;
        var currentValueOwner = currentValue;

        for (var i = 0; i < scopePath.Length; i++)
        {
            var s = scopePath[i];

            currentValueOwner = currentValue;

            if (i == 0)
            {
                currentProperty = typeof(ReportScopeSet).GetProperty(s, BindingAttr);
            }
            else if (currentProperty is not null)
            {
                currentProperty = currentProperty.PropertyType.GetProperty(s, BindingAttr);
            }
            else
            {
                throw new Exception($"Index {i} has null property.");
            }

            if (currentProperty is null)
            {
                var type = currentValueOwner.GetType();

                if (!type.IsSubclassOf(typeof(ReportScopeWithParam)))
                {
                    removedScope = null;
                    return false;
                }

                var paramProp = type.GetProperty(nameof(ReportScopeWithParam.Param)) ?? throw new Exception("Param couldn't be found");

                if (paramProp.GetValue(currentValueOwner) is not string[] param)
                {
                    removedScope = null;
                    return false;
                }

                var actualParamName = default(string);

                foreach (var singleParam in param)
                {
                    if (string.Equals(singleParam, s, StringComparison.OrdinalIgnoreCase))
                    {
                        actualParamName = singleParam;
                        break;
                    }
                }
                
                if (actualParamName is null)
                {
                    removedScope = null;
                    return false;
                }

                var newParam = param
                    .Where(x => !string.Equals(x, actualParamName))
                    .ToArray();

                paramProp.SetValue(currentValueOwner, newParam.Length == 0 ? null : newParam);

                exactScopeList.Add(actualParamName);
                
                removedScope = string.Join(':', exactScopeList);
                return true;
            }

            currentValue = currentProperty.GetValue(currentValueOwner) as ReportScope;

            if (currentValue is null)
            {
                removedScope = null;
                return false;
            }

            exactScopeList.Add(currentProperty.Name);
        }

        currentProperty?.SetValue(currentValueOwner, null);

        removedScope = string.Join(':', exactScopeList);
        return true;
    }

    public static bool TryParse(
        [NotNullWhen(true)] string? scope,
        [NotNullWhen(true)] out ReportScopeSet? reportScopeSet,
        [NotNullWhen(true)] out string? addedScope,
        out string? specificValueIssue)
    {
        if (scope is null)
        {
            reportScopeSet = null;
            addedScope = null;
            specificValueIssue = null;
            return false;
        }

        var addedScopeList = new List<string>();

        var scopeSet = new ReportScopeSet();

        var scopePath = scope.Split(':');
        
        var subScopeOwner = (ReportScope)scopeSet;
        var currentProperty = default(PropertyInfo);
        var currentSubScope = default(ReportScope);

        for (var i = 0; i < scopePath.Length; i++)
        {
            var s = scopePath[i];

            if (i == 0)
            {
                currentProperty = typeof(ReportScopeSet).GetProperty(s, BindingAttr);
            }
            else if (currentProperty is not null)
            {
                currentProperty = currentProperty.PropertyType.GetProperty(s, BindingAttr);
            }
            else
            {
                throw new Exception($"Index {i} has null property.");
            }

            if (currentProperty is null)
            {
                if (currentSubScope is null)
                {
                    // When the scope is not correct at the very first layer
                    reportScopeSet = null;
                    addedScope = null;
                    specificValueIssue = null;
                    return false;
                }

                var type = currentSubScope.GetType();

                if (!type.IsSubclassOf(typeof(ReportScopeWithParam)))
                {
                    // Scope is partically correct, but not completely
                    reportScopeSet = null;
                    addedScope = null;
                    specificValueIssue = s;
                    return false;
                }

                var paramProp = type.GetProperty(nameof(ReportScopeWithParam.Param)) ?? throw new Exception("Param couldn't be found");
                
                addedScopeList.Add(s);

                paramProp.SetValue(currentSubScope, new string[] { s });

                continue; // or break?
            }
            
            currentSubScope = Activator.CreateInstance(currentProperty.PropertyType) as ReportScope;
            currentProperty.SetValue(subScopeOwner, currentSubScope);
            subScopeOwner = currentSubScope;
            
            addedScopeList.Add(currentProperty.Name);
        }

        addedScope = string.Join(':', addedScopeList);
        reportScopeSet = scopeSet;
        specificValueIssue = null;
        
        return true;
    }
}
