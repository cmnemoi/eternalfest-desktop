using EternalfestDesktop.Infrastructure.FileSystem;

namespace EternalfestDesktop.Tests.Infrastructure.FileSystem;

public sealed class AppFilesTest
{
    /// @spec packaging::macos-app
    [Fact]
    public void Finds_its_files_in_the_resources_of_a_mac_app_bundle()
    {
        var folder = AppFiles.FolderOf("/Applications/Eternalfest Desktop.app/Contents/MacOS/");

        Assert.Equal("/Applications/Eternalfest Desktop.app/Contents/Resources", folder);
    }

    /// @spec packaging::self-contained-archives
    [Theory]
    [InlineData("/home/player/Jeux/EternalfestDesktop-0.5.0-linux-x64/")]
    [InlineData("/tmp/.mount_eternaXyZ123/usr/bin/")]
    [InlineData("/Users/player/eternalfest-desktop/src/EternalfestDesktop.Ui/bin/Debug/net10.0/")]
    public void Finds_its_files_next_to_its_executable_anywhere_else(string appDirectory)
    {
        Assert.Equal(appDirectory, AppFiles.FolderOf(appDirectory));
    }
}
