using CommunityToolkit.Mvvm.ComponentModel;
using EternalfestDesktop.Application;

namespace EternalfestDesktop.Ui.ViewModels;

/// <summary>Navigates between the library and a contrée's page.</summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly Func<ContreeCardViewModel, Action, ContreePageViewModel> _contreePage;

    public MainWindowViewModel(GameCatalog catalog, GameStore store, ContreeIcons icons, Func<ContreeCardViewModel, Action, ContreePageViewModel> contreePage)
    {
        _contreePage = contreePage;
        Library = new LibraryViewModel(catalog, store, icons, Open);
        CurrentPage = Library;
    }

    public LibraryViewModel Library { get; }

    [ObservableProperty]
    public partial ObservableObject CurrentPage { get; set; }

    private void Open(ContreeCardViewModel contree)
    {
        var page = _contreePage(contree, () => CurrentPage = Library);
        CurrentPage = page;
        _ = page.Load(CancellationToken.None);
    }
}
