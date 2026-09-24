using System.Reflection;
using System.Text.Json;

namespace EternalfestDesktop.Infrastructure.EternalfestApi;

internal static class EternalfestJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
    };

    public static readonly string UserAgent =
        $"EternalfestDesktop/{typeof(EternalfestJson).Assembly.GetName().Version?.ToString(3)} (unofficial offline launcher)";
}
