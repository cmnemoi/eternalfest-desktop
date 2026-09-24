using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EternalfestDesktop.Application;
using EternalfestDesktop.Domain;
using EternalfestDesktop.Ui.Resources;

namespace EternalfestDesktop.Ui.ViewModels;

/// <summary>Every contrée the player can play: the public catalog, or the downloaded ones when offline.</summary>
public sealed partial class LibraryViewModel(GameCatalog catalog, GameStore store, ContreeIcons icons, Action<ContreeCardViewModel> open) : ObservableObject
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

    [RelayCommand]
    private void Open(ContreeCardViewModel contree) => open(contree);

    /// @spec catalog::lists-public-contrees
    /// @spec catalog::first-launch-without-network
    [RelayCommand]
    public async Task Load(CancellationToken cancellationToken)
    {
        IsLoading = true;
        try
        {
            var downloaded = (await store.ListGames(cancellationToken)).ToDictionary(game => game.Id);
            IEnumerable<ContreeCardViewModel> cards;
            try
            {
                cards = (await catalog.ListPublicGames(cancellationToken)).Select(entry => Card(entry, downloaded));
                IsOffline = false;
            }
            catch (EternalfestUnreachableException)
            {
                cards = downloaded.Values.Select(game => Card(
                    new CatalogEntry(game.Id, game.Key, game.Build.Version, game.DisplayName, game.Description, game.Build.Icon),
                    downloaded));
                IsOffline = true;
            }
            _all = cards.ToList();
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

    private static ContreeCardViewModel Card(CatalogEntry entry, Dictionary<GameId, Game> downloaded) =>
        new(entry.Id, Text.Localized(entry.DisplayName), Text.Localized(entry.Description), entry.Version, entry.Icon)
        {
            IsDownloaded = downloaded.ContainsKey(entry.Id),
            IsUpdateAvailable = downloaded.TryGetValue(entry.Id, out var game) && game.Build.Version != entry.Version,
        };

    private async Task LoadIcon(ContreeCardViewModel card, CancellationToken cancellationToken)
    {
        if (card.IconBlob is { } blob)
            card.Icon = await icons.Load(blob, cancellationToken);
    }
}
