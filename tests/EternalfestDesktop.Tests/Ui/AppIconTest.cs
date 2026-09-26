using EternalfestDesktop.Tests.Support;

namespace EternalfestDesktop.Tests.Ui;

public sealed class AppIconTest
{
    /// @spec ui::app-icon
    [Fact]
    public void Holds_every_size_windows_uses()
    {
        using var icon = new BinaryReader(File.OpenRead(RepositoryFile.PathOf("src/EternalfestDesktop.Ui/Assets/icon.ico")));

        Assert.Equal(0, icon.ReadUInt16());
        Assert.Equal(1, icon.ReadUInt16());
        var sizes = Enumerable.Range(0, icon.ReadUInt16()).Select(_ =>
        {
            var width = icon.ReadByte();
            icon.ReadBytes(15);
            return width == 0 ? 256 : width;
        });
        Assert.Equal([16, 24, 32, 48, 64, 128, 256], sizes.Order());
    }

    /// @spec ui::app-icon
    [Fact]
    public void Is_the_icon_of_the_window_and_the_executable()
    {
        Assert.Contains("Icon=\"/Assets/icon.png\"", File.ReadAllText(RepositoryFile.PathOf("src/EternalfestDesktop.Ui/Views/MainWindow.axaml")), StringComparison.Ordinal);
        Assert.Contains("<ApplicationIcon>Assets\\icon.ico</ApplicationIcon>", File.ReadAllText(RepositoryFile.PathOf("src/EternalfestDesktop.Ui/EternalfestDesktop.Ui.csproj")), StringComparison.Ordinal);
        Assert.True(File.Exists(RepositoryFile.PathOf("src/EternalfestDesktop.Ui/Assets/icon.png")));
    }
}
