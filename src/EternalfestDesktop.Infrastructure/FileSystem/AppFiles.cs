namespace EternalfestDesktop.Infrastructure.FileSystem;

/// <summary>Where the files the app ships with are: the Flash files, the Flash players, the icon and the license notices.</summary>
public static class AppFiles
{
    public static string Folder { get; } = FolderOf(AppContext.BaseDirectory);

    /// <summary>Next to the executable, except in a macOS app bundle, which keeps them in <c>Contents/Resources</c>.</summary>
    /// <param name="appDirectory">The folder of the app's executable.</param>
    /// @spec packaging::macos-app
    public static string FolderOf(string appDirectory)
    {
        var executables = new DirectoryInfo(appDirectory);
        return IsInMacAppBundle(executables) ? Path.Combine(executables.Parent!.FullName, "Resources") : appDirectory;
    }

    private static bool IsInMacAppBundle(DirectoryInfo executables) =>
        executables is { Name: "MacOS", Parent: { Name: "Contents", Parent: { } bundle } } && bundle.Name.EndsWith(".app", StringComparison.Ordinal);
}
