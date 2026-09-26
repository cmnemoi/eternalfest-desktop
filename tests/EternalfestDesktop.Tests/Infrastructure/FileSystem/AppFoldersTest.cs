using EternalfestDesktop.Infrastructure.FileSystem;

namespace EternalfestDesktop.Tests.Infrastructure.FileSystem;

public sealed class AppFoldersTest
{
    public static bool IsMacOS => OperatingSystem.IsMacOS();

    /// @spec packaging::data-outside-install
    [Fact(Skip = "The macOS data folder", SkipUnless = nameof(IsMacOS))]
    public void Keeps_its_data_in_the_application_support_folder_on_macos()
    {
        var applicationSupport = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support");

        Assert.Equal(Path.Combine(applicationSupport, "EternalfestDesktop"), AppFolders.ForThisUser.Data);
    }
}
