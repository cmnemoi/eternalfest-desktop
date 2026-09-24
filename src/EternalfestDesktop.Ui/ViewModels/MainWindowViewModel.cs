using CommunityToolkit.Mvvm.ComponentModel;
using EternalfestDesktop.Application;

namespace EternalfestDesktop.Ui.ViewModels;

/// <summary>Navigates between the library, a contrée's page and the settings.</summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly Func<ContreeCardViewModel, Action, ContreePageViewModel> _contreePage;
    private readonly Func<Action, SettingsViewModel> _settings;

    public MainWindowViewModel(
        BrowseCatalog browseCatalog,
        ContreeIcons icons,
        PreferencesFile preferences,
        Func<ContreeCardViewModel, Action, ContreePageViewModel> contreePage,
        Func<Action, SettingsViewModel> settings)
    {
        _contreePage = contreePage;
        _settings = settings;
        Library = new LibraryViewModel(browseCatalog, icons, preferences, Open, OpenSettings);
        CurrentPage = Library;
    }

    public LibraryViewModel Library { get; }

    [ObservableProperty]
    public partial ObservableObject CurrentPage { get; set; }

    private void Open(ContreeCardViewModel contree, bool play)
    {
        var page = _contreePage(contree, BackToLibrary);
        CurrentPage = page;
        _ = OpenPage(page, play);
    }

    private static async Task OpenPage(ContreePageViewModel page, bool play)
    {
        await page.Load(CancellationToken.None);
        if (play && page.PlayCommand.CanExecute(null))
            await page.PlayCommand.ExecuteAsync(null);
    }

    private void OpenSettings() => CurrentPage = _settings(BackToLibrary);

    private void BackToLibrary()
    {
        CurrentPage = Library;
        _ = Library.Load(CancellationToken.None);
    }
}
