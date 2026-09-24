namespace EternalfestDesktop.Infrastructure.LocalServer;

/// <summary>The Eternalfest loader and base engine shipped with the app (see ADR 0005 and assets/flash/NOTICE.md).</summary>
public sealed class BundledFlashFiles(string folder)
{
    /// <summary>Version of the bundled <c>loader.swf</c>, from <c>@eternalfest/loader</c>.</summary>
    public static readonly Version LoaderVersion = new(5, 1, 2);

    public static BundledFlashFiles NextToApp() => new(Path.Combine(AppContext.BaseDirectory, "flash"));

    public Stream OpenLoader() => File.OpenRead(Path.Combine(folder, "loader.swf"));

    public Stream OpenBaseEngine() => File.OpenRead(Path.Combine(folder, "game.swf"));
}
