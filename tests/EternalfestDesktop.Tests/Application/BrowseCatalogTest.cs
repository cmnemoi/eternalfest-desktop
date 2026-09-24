using EternalfestDesktop.Tests.Support;

namespace EternalfestDesktop.Tests.Application;

public sealed class BrowseCatalogTest : IDisposable
{
    private readonly TestLauncher _launcher = new();

    public void Dispose() => _launcher.Dispose();

    /// @spec catalog::last-known-catalog-offline
    [Fact]
    public async Task Shows_the_last_catalog_when_eternalfest_is_unreachable()
    {
        Publish("Dojo", "Himmelen");
        await Browse();
        _launcher.Eternalfest.IsUnreachable = true;

        var library = await Browse();

        Assert.True(library.IsOffline);
        Assert.Equal(["Dojo", "Himmelen"], library.Entries.Select(entry => entry.Contree.DisplayName.Default));
    }

    /// @spec catalog::last-known-catalog-offline
    [Fact]
    public async Task A_successful_refresh_replaces_the_saved_catalog()
    {
        Publish("Dojo");
        await Browse();
        Publish("Himmelen");
        await Browse();
        _launcher.Eternalfest.IsUnreachable = true;

        var library = await Browse();

        Assert.Equal(["Dojo", "Himmelen"], library.Entries.Select(entry => entry.Contree.DisplayName.Default));
    }

    /// @spec catalog::first-launch-without-network
    [Fact]
    public async Task Shows_only_downloaded_contrees_when_no_catalog_was_ever_saved()
    {
        var dojo = Publish("Dojo")[0];
        Publish("Himmelen");
        await _launcher.Download(dojo);
        _launcher.Eternalfest.IsUnreachable = true;

        var library = await Browse();

        Assert.True(library.IsOffline);
        var entry = Assert.Single(library.Entries);
        Assert.Equal("Dojo", entry.Contree.DisplayName.Default);
        Assert.True(entry.IsDownloaded);
    }

    /// @spec catalog::first-launch-without-network
    [Fact]
    public async Task Shows_nothing_on_a_fresh_install_without_network()
    {
        Publish("Dojo");
        _launcher.Eternalfest.IsUnreachable = true;

        var library = await Browse();

        Assert.True(library.IsOffline);
        Assert.Empty(library.Entries);
    }

    /// @spec catalog::lists-public-contrees
    [Fact]
    public async Task Keeps_a_downloaded_contree_no_longer_listed()
    {
        var dojo = Publish("Dojo")[0];
        await _launcher.Download(dojo);
        _launcher.Eternalfest.Unlisting(dojo);

        var library = await Browse();

        Assert.Equal(["Dojo"], library.Entries.Select(entry => entry.Contree.DisplayName.Default));
    }

    private List<PublishedContree> Publish(params string[] names)
    {
        var contrees = names.Select(PublishedContree.Named).ToList();
        foreach (var contree in contrees)
            _launcher.Eternalfest.Publishing(contree);
        return contrees;
    }

    private Task<EternalfestDesktop.Application.Library> Browse() =>
        _launcher.BrowseCatalog.Execute(TestContext.Current.CancellationToken);
}
