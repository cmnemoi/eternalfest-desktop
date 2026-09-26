using EternalfestDesktop.Domain;
using EternalfestDesktop.Tests.Support;

namespace EternalfestDesktop.Tests.Application;

public sealed class FetchIconTest : IDisposable
{
    private readonly TestLauncher _launcher = new();

    public void Dispose() => _launcher.Dispose();

    /// @spec ui::library-icons
    [Fact]
    public async Task Shows_the_icon_eternalfest_publishes()
    {
        var contree = PublishedContree.Named("Dojo");
        _launcher.Eternalfest.Publishing(contree);

        var icon = await FetchIconOf(contree);

        Assert.Equal(contree.Blobs[contree.BlobOf("icon")], icon);
    }

    /// @spec ui::library-icons
    [Fact]
    public async Task Downloads_an_icon_once()
    {
        var contree = PublishedContree.Named("Dojo");
        _launcher.Eternalfest.Publishing(contree);
        var iconBlob = await IconBlobOf(contree);
        await FetchIcon(iconBlob);
        _launcher.Eternalfest.ReceivedRequests.Clear();

        var icon = await FetchIcon(iconBlob);

        Assert.Equal(contree.Blobs[contree.BlobOf("icon")], icon);
        Assert.Empty(_launcher.Eternalfest.ReceivedRequests);
    }

    /// @spec ui::library-icons
    [Fact]
    public async Task Shows_no_icon_while_eternalfest_is_unreachable_then_downloads_it_once_back()
    {
        var contree = PublishedContree.Named("Dojo");
        _launcher.Eternalfest.Publishing(contree);
        var iconBlob = await IconBlobOf(contree);
        _launcher.Eternalfest.IsUnreachable = true;
        Assert.Null(await FetchIcon(iconBlob));

        _launcher.Eternalfest.IsUnreachable = false;
        var icon = await FetchIcon(iconBlob);

        Assert.Equal(contree.Blobs[contree.BlobOf("icon")], icon);
    }

    /// @spec ui::library-icons
    [Fact]
    public async Task Shows_no_icon_when_eternalfest_no_longer_publishes_it()
    {
        var contree = PublishedContree.Named("Dojo");
        _launcher.Eternalfest.Publishing(contree).Withdrawing(contree.BlobOf("icon"));

        var icon = await FetchIconOf(contree);

        Assert.Null(icon);
        Assert.False(_launcher.Store.HasBlob(contree.BlobOf("icon")));
    }

    /// @spec ui::library-icons
    /// @spec store::verifies-blob-digest
    [Fact]
    public async Task Refuses_an_icon_that_doesnt_match_its_digest()
    {
        var contree = PublishedContree.Named("Dojo").WithCorruptedIcon();
        _launcher.Eternalfest.Publishing(contree);

        var icon = await FetchIconOf(contree);

        Assert.Null(icon);
        Assert.False(_launcher.Store.HasBlob(contree.BlobOf("icon")));
    }

    private async Task<byte[]?> FetchIconOf(PublishedContree contree) => await FetchIcon(await IconBlobOf(contree));

    private async Task<Blob> IconBlobOf(PublishedContree contree)
    {
        var catalog = await _launcher.Catalog.ListPublicGames(TestContext.Current.CancellationToken);
        return catalog.Single(entry => entry.Id == contree.Id).Icon!;
    }

    private async Task<byte[]?> FetchIcon(Blob icon)
    {
        await using var stream = await _launcher.FetchIcon.Execute(icon, TestContext.Current.CancellationToken);
        if (stream is null)
            return null;
        using var bytes = new MemoryStream();
        await stream.CopyToAsync(bytes, TestContext.Current.CancellationToken);
        return bytes.ToArray();
    }
}
