using System.Text.Json;
using System.Text.Json.Serialization;

namespace WslQuery;

internal static class QueryJson
{
    private static readonly QueryJsonContext PrettyContext = new(new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    });

    internal static string Serialize(List<DistroInfo> results, bool pretty) =>
        JsonSerializer.Serialize(results, (pretty ? PrettyContext : QueryJsonContext.Default).ListDistroInfo);
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(List<DistroInfo>))]
internal partial class QueryJsonContext : JsonSerializerContext;
