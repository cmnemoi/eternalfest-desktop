using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EternalfestDesktop.Application;
using EternalfestDesktop.Ui.Resources;

namespace EternalfestDesktop.Ui.ViewModels;

/// <summary>Every contrée the player can play: the public catalog, or the downloaded ones when offline.</summary>
public sealed partial class LibraryViewModel(
    BrowseCatalog browseCatalog,
    ContreeIcons icons,
    PreferencesFile preferences,
    Func<ContreeCardViewModel, bool, Task> open,
    Action openSettings) : ObservableObject
{
    private IReadOnlyList<ContreeCardViewModel> _all = [];

    public ObservableCollection<ContreeCardViewModel> Contrees { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NoMatchMessage), nameof(HasNoMatch))]
    public partial string SearchText { get; set; } = "";

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    /// @spec catalog::first-launch-without-network
    [ObservableProperty]
    public partial bool IsOffline { get; set; }

    public bool HasNoMatch => !IsLoading && Contrees.Count == 0 && SearchText.Trim().Length > 0;
    public string NoMatchMessage => Text.Format(Strings.NoMatch, SearchText.Trim());

    /// <summary>The last contrée played, while it is still downloaded.</summary>
    /// @spec ui::play-again
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlayAgainLabel))]
    [NotifyCanExecuteChangedFor(nameof(PlayAgainCommand))]
    public partial ContreeCardViewModel? LastPlayed { get; set; }

    public string PlayAgainLabel => LastPlayed is null ? "" : Text.Format(Strings.PlayAgain, LastPlayed.DisplayName);

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task Open(ContreeCardViewModel contree) => open(contree, false);

    [RelayCommand(CanExecute = nameof(CanPlayAgain), AllowConcurrentExecutions = true)]
    private Task PlayAgain() => open(LastPlayed!, true);

    private bool CanPlayAgain() => LastPlayed is not null;

    [RelayCommand]
    private void OpenSettings() => openSettings();

    /// @spec catalog::lists-public-contrees
    /// @spec catalog::last-known-catalog-offline
    [RelayCommand]
    public async Task Load(CancellationToken cancellationToken)
    {
        IsLoading = true;
        try
        {
            var library = await browseCatalog.Execute(cancellationToken);
            IsOffline = library.IsOffline;
            _all = library.Entries.Select(Card).ToList();
            LastPlayed = _all.FirstOrDefault(card => card.IsDownloaded && card.Id.Value == preferences.Current.LastPlayed);
            Filter();
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasNoMatch));
        }
        foreach (var card in _all)
            await LoadIcon(card, cancellationToken);
    }

    /// @spec ui::library
    partial void OnSearchTextChanged(string value) => Filter();

    private void Filter()
    {
        var terms = Text.Searchable(SearchText).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Contrees.Clear();
        foreach (var card in _all.Where(card => terms.All(card.SearchableText.Contains)))
            Contrees.Add(card);
    }

    private static ContreeCardViewModel Card(LibraryEntry entry) =>
        new(entry.Contree.Id, Text.Localized(entry.Contree.DisplayName), Text.Localized(entry.Contree.Description), entry.Contree.Version, entry.Contree.Icon)
        {
            IsDownloaded = entry.IsDownloaded,
            IsUpdateAvailable = entry.IsUpdateAvailable,
        };

    private async Task LoadIcon(ContreeCardViewModel card, CancellationToken cancellationToken)
    {
        if (card.IconBlob is { } blob)
            card.Icon = await icons.Load(blob, cancellationToken);
    }
}
