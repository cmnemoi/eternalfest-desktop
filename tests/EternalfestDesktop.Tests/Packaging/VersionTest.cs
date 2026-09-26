using EternalfestDesktop.Application;
using EternalfestDesktop.Infrastructure.LocalServer;
using EternalfestDesktop.Ui.ViewModels;

namespace EternalfestDesktop.Tests.Packaging;

public sealed class VersionTest
{
    /// @spec packaging::one-version
    [Theory]
    [InlineData(typeof(MainWindowViewModel))]
    [InlineData(typeof(PlayGame))]
    [InlineData(typeof(KestrelOfflineBackend))]
    public void Builds_the_app_in_the_version_release_please_maintains(Type fromAssembly)
    {
        Assert.Equal(ReleasedVersion(), fromAssembly.Assembly.GetName().Version?.ToString(3));
    }

    private static string ReleasedVersion()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
            if (File.Exists(Path.Combine(folder.FullName, "version.txt")))
                return File.ReadAllText(Path.Combine(folder.FullName, "version.txt")).Trim();
        throw new FileNotFoundException("version.txt isn't in any folder above the tests.");
    }
}
