using EternalfestDesktop.Tests.Support;
using EternalfestDesktop.Ui.ViewModels;

namespace EternalfestDesktop.Tests.Ui;

public sealed class LibraryViewModelTest : IDisposable
{
    private readonly TestLauncher _launcher = new();

    public void Dispose() => _launcher.Dispose();

    /// @spec ui::no-first-launch-setup
    /// @spec catalog::lists-public-contrees
    [Fact]
    public async Task Lists_the_catalog_without_any_prior_step()
    {
        Publish("Les Cavernes de Hammerfest", "Dojo");

        var library = await OpenLibrary();

        Assert.Equal(["Les Cavernes de Hammerfest", "Dojo"], Names(library));
        Assert.False(library.IsOffline);
    }

    /// @spec ui::library
    [Theory]
    [InlineData("cavernes", "Les Cavernes de Hammerfest")]
    [InlineData("élite", "Elite Force")]
    [InlineData("ELITE", "Elite Force")]
    [InlineData("hammerfest cavernes", "Les Cavernes de Hammerfest")]
    [InlineData("Description of Dojo", "Dojo")]
    public async Task Searches_names_and_descriptions_ignoring_case_and_accents(string search, string found)
    {
        Publish("Les Cavernes de Hammerfest", "Elite Force", "Dojo");
        var library = await OpenLibrary();

        library.SearchText = search;

        Assert.Equal([found], Names(library));
        Assert.False(library.HasNoMatch);
    }

    /// @spec ui::library
    [Fact]
    public async Task Says_when_no_contree_matches()
    {
        Publish("Dojo");
        var library = await OpenLibrary();

        library.SearchText = "zzz";

        Assert.Empty(library.Contrees);
        Assert.True(library.HasNoMatch);
        Assert.Contains("zzz", library.NoMatchMessage, StringComparison.Ordinal);
    }

    /// @spec ui::library
    [Fact]
    public async Task Badges_downloaded_contrees()
    {
        var (dojo, _) = Publish("Dojo", "Himmelen");
        await _launcher.Download(dojo);

        var library = await OpenLibrary();

        Assert.Equal([true, false], library.Contrees.Select(contree => contree.IsDownloaded));
        Assert.All(library.Contrees, contree => Assert.False(contree.IsUpdateAvailable));
    }

    /// @spec store::detects-newer-build
    [Fact]
    public async Task Badges_downloaded_contrees_with_a_newer_build()
    {
        var dojo = PublishedContree.Named("Dojo").InVersion("1.0.0");
        _launcher.Eternalfest.Publishing(dojo);
        await _launcher.Download(dojo);
        _launcher.Eternalfest.Publishing(dojo.InVersion("2.0.0"));

        var library = await OpenLibrary();

        var card = Assert.Single(library.Contrees);
        Assert.True(card.IsDownloaded);
        Assert.True(card.IsUpdateAvailable);
    }

    /// @spec catalog::first-launch-without-network
    [Fact]
    public async Task Shows_downloaded_contrees_when_eternalfest_is_unreachable()
    {
        var (dojo, _) = Publish("Dojo", "Himmelen");
        await _launcher.Download(dojo);
        _launcher.Eternalfest.IsUnreachable = true;

        var library = await OpenLibrary();

        Assert.True(library.IsOffline);
        Assert.Equal(["Dojo"], Names(library));
    }

    /// @spec catalog::first-launch-without-network
    [Fact]
    public async Task Retrying_once_eternalfest_is_back_lists_the_catalog()
    {
        Publish("Dojo", "Himmelen");
        _launcher.Eternalfest.IsUnreachable = true;
        var library = await OpenLibrary();
        Assert.Empty(library.Contrees);

        _launcher.Eternalfest.IsUnreachable = false;
        await library.LoadCommand.ExecuteAsync(null);

        Assert.False(library.IsOffline);
        Assert.Equal(2, library.Contrees.Count);
    }

    private (PublishedContree, PublishedContree?) Publish(params string[] names)
    {
        var contrees = names.Select(PublishedContree.Named).ToList();
        foreach (var contree in contrees)
            _launcher.Eternalfest.Publishing(contree);
        return (contrees[0], contrees.ElementAtOrDefault(1));
    }

    private async Task<LibraryViewModel> OpenLibrary()
    {
        var library = _launcher.MainWindow().Library;
        await library.Load(TestContext.Current.CancellationToken);
        return library;
    }

    private static string[] Names(LibraryViewModel library) => library.Contrees.Select(contree => contree.DisplayName).ToArray();
}
