namespace EternalfestDesktop.Infrastructure.LocalServer;

/// <summary>The Eternalfest loader and base engine shipped with the app (see ADR 0005 and assets/flash/NOTICE.md).</summary>
public sealed class BundledFlashFiles(string folder)
{
    public static BundledFlashFiles NextToApp() => new(Path.Combine(AppContext.BaseDirectory, "flash"));

    public Stream OpenLoader() => File.OpenRead(Path.Combine(folder, "loader.swf"));

    public Stream OpenBaseEngine() => File.OpenRead(Path.Combine(folder, "game.swf"));
}
