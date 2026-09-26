using EternalfestDesktop.Application;
using EternalfestDesktop.Infrastructure.LocalServer;
using EternalfestDesktop.Tests.Support;
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

    private static string ReleasedVersion() => File.ReadAllText(RepositoryFile.PathOf("version.txt")).Trim();
}
