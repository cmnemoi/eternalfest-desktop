using System.Text.Json;
using System.Text.Json.Nodes;

namespace EternalfestDesktop.Tests.Support;

/// <summary>The structure of a JSON document (property names and value kinds), without its values.</summary>
internal static class JsonShape
{
    /// <summary>Properties whose keys are data (item ids, locales, mode keys), compared as maps.</summary>
    private static readonly HashSet<string> Maps = ["items", "stats", "i18n", "modes", "options"];

    public static string Of(JsonNode? node, string? property = null) => node switch
    {
        null => "null",
        JsonObject when property is not null && Maps.Contains(property) => "map",
        JsonObject obj => "{" + string.Join(",", obj.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => $"{pair.Key}:{Of(pair.Value, pair.Key)}")) + "}",
        JsonArray array => "[" + (array.Count == 0 ? "" : Of(array[0])) + "]",
        _ => node.GetValueKind() switch
        {
            JsonValueKind.True or JsonValueKind.False => "bool",
            var kind => kind.ToString().ToLowerInvariant(),
        },
    };
}
