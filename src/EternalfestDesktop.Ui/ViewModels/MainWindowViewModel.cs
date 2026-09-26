using CommunityToolkit.Mvvm.ComponentModel;
using EternalfestDesktop.Application;

namespace EternalfestDesktop.Ui.ViewModels;

/// <summary>Navigates between the library, a contrée's page and the settings.</summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly Func<ContreeCardViewModel, Func<Task>, ContreePageViewModel> _contreePage;
    private readonly Func<Func<Task>, SettingsViewModel> _settings;

    public MainWindowViewModel(
        BrowseCatalog browseCatalog,
        ContreeIcons icons,
        PreferencesFile preferences,
        Func<ContreeCardViewModel, Func<Task>, ContreePageViewModel> contreePage,
        Func<Func<Task>, SettingsViewModel> settings)
    {
        _contreePage = contreePage;
        _settings = settings;
        Library = new LibraryViewModel(browseCatalog, icons, preferences, Open, OpenSettings);
        CurrentPage = Library;
    }

    public LibraryViewModel Library { get; }

    [ObservableProperty]
    public partial ObservableObject CurrentPage { get; set; }

    /// <summary>Shows the contrée's page at once; the returned task ends once it is loaded, and played if asked.</summary>
    private Task Open(ContreeCardViewModel contree, bool play)
    {
        var page = _contreePage(contree, BackToLibrary);
        CurrentPage = page;
        return OpenPage(page, play);
    }

    private static async Task OpenPage(ContreePageViewModel page, bool play)
    {
        await page.Load(CancellationToken.None);
        if (play && page.PlayCommand.CanExecute(null))
            await page.PlayCommand.ExecuteAsync(null);
    }

    private void OpenSettings() => CurrentPage = _settings(BackToLibrary);

    /// <summary>Shows the library at once; the returned task ends once it is reloaded.</summary>
    private Task BackToLibrary()
    {
        CurrentPage = Library;
        return Library.Load(CancellationToken.None);
    }
}
