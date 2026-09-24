using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using EternalfestDesktop.Ui.Resources;
using EternalfestDesktop.Ui.ViewModels;

namespace EternalfestDesktop.Ui.Views;

public sealed partial class SettingsView : UserControl
{
    public SettingsView() => InitializeComponent();

    private async void ChangeCacheFolder(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel settings || TopLevel.GetTopLevel(this)?.StorageProvider is not { } storage)
            return;
        var folders = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = Strings.CacheFolder });
        if (folders.Count > 0 && folders[0].TryGetLocalPath() is { } path)
            settings.ChangeCacheFolder(path);
    }
}
