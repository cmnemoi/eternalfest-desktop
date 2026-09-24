using System.Text.Json.Nodes;

namespace EternalfestDesktop.Tests.Support;

internal static class Fixture
{
    public static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", relativePath));

    public static JsonObject ReadObject(string relativePath) =>
        JsonNode.Parse(Read(relativePath))!.AsObject();
}
