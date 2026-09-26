namespace EternalfestDesktop.Tests.Support;

/// <summary>A file of this repository, found from the folder the tests run in.</summary>
internal static class RepositoryFile
{
    /// <exception cref="FileNotFoundException">No folder above the tests holds <paramref name="relativePath" />.</exception>
    public static string PathOf(string relativePath)
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
            if (File.Exists(Path.Combine(folder.FullName, relativePath)))
                return Path.Combine(folder.FullName, relativePath);
        throw new FileNotFoundException($"{relativePath} isn't in any folder above the tests.");
    }
}
