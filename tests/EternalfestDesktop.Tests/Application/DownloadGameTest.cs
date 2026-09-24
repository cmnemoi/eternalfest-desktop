using EternalfestDesktop.Application;
using EternalfestDesktop.Domain;
using EternalfestDesktop.Tests.Support;

namespace EternalfestDesktop.Tests.Application;

public sealed class DownloadGameTest : IDisposable
{
    private readonly TestLauncher _launcher = new();

    public void Dispose() => _launcher.Dispose();

    /// @spec store::downloads-complete-build
    [Fact]
    public async Task Downloads_every_file_of_the_active_build()
    {
        var contree = PublishedContree.Named("Accumulation");
        _launcher.Eternalfest.Publishing(contree);

        await _launcher.Download(contree);

        var downloaded = await _launcher.FindDownloaded(contree);
        Assert.NotNull(downloaded);
        Assert.Equal(7, downloaded.Build.Blobs().Count);
        Assert.All(contree.Blobs, blob => Assert.Equal(blob.Value, ReadCached(blob.Key)));
    }

    /// @spec store::downloads-complete-build
    [Fact]
    public async Task Downloads_a_contree_running_on_the_base_engine()
    {
        var contree = PublishedContree.Named("Temple des boss").OnBaseEngine();
        _launcher.Eternalfest.Publishing(contree);

        var game = await _launcher.Download(contree);

        Assert.IsType<BaseEngine>(game.Build.Engine);
        Assert.Equal(6, game.Build.Blobs().Count);
    }

    /// @spec store::verifies-blob-digest
    /// @spec store::downloaded-only-when-complete
    [Fact]
    public async Task Refuses_a_blob_that_doesnt_match_its_digest()
    {
        var contree = PublishedContree.Named("Trolilol").WithCorruptedContent();
        _launcher.Eternalfest.Publishing(contree);

        await Assert.ThrowsAsync<CorruptedBlobException>(() => _launcher.Download(contree));

        Assert.Null(await _launcher.FindDownloaded(contree));
        Assert.False(_launcher.Store.HasBlob(contree.BlobOf("content")));
    }

    /// @spec store::downloaded-only-when-complete
    [Fact]
    public async Task Resumes_an_interrupted_download_without_fetching_verified_blobs_again()
    {
        var contree = PublishedContree.Named("Himmelen");
        _launcher.Eternalfest.Publishing(contree);
        _launcher.Eternalfest.DropsConnectionAtBlob = contree.BlobOf("content");
        await Assert.ThrowsAsync<EternalfestUnreachableException>(() => _launcher.Download(contree));
        Assert.Null(await _launcher.FindDownloaded(contree));

        _launcher.Eternalfest.DropsConnectionAtBlob = null;
        _launcher.Eternalfest.ReceivedRequests.Clear();
        await _launcher.Download(contree);

        Assert.NotNull(await _launcher.FindDownloaded(contree));
        Assert.Equal(5, _launcher.Eternalfest.BlobDownloads.Count());
    }

    /// @spec store::never-downloads-twice
    [Fact]
    public async Task Downloading_a_downloaded_contree_sends_no_request()
    {
        var contree = PublishedContree.Named("Dojo");
        _launcher.Eternalfest.Publishing(contree);
        await _launcher.Download(contree);
        _launcher.Eternalfest.ReceivedRequests.Clear();

        await _launcher.Download(contree);

        Assert.Empty(_launcher.Eternalfest.ReceivedRequests);
    }

    /// @spec store::never-downloads-twice
    [Fact]
    public async Task Downloads_a_blob_shared_between_contrees_once()
    {
        var sharedMusic = new BlobId(Guid.NewGuid());
        var music = PublishedContree.Bytes(24);
        var first = PublishedContree.Named("Échiquier").WithMusic(sharedMusic, music);
        var second = PublishedContree.Named("Rareraland").WithMusic(sharedMusic, music);
        _launcher.Eternalfest.Publishing(first).Publishing(second);
        await _launcher.Download(first);
        _launcher.Eternalfest.ReceivedRequests.Clear();

        await _launcher.Download(second);

        Assert.DoesNotContain(_launcher.Eternalfest.BlobDownloads, request => request.RequestUri!.AbsolutePath.Contains(sharedMusic.ToString(), StringComparison.Ordinal));
    }

    /// @spec store::reports-progress
    [Fact]
    public async Task Reports_downloaded_bytes_out_of_the_build_size()
    {
        var contree = PublishedContree.Named("Wicked game");
        _launcher.Eternalfest.Publishing(contree);

        await _launcher.Download(contree);

        Assert.All(_launcher.ReportedProgress, progress => Assert.Equal(contree.ByteSize, progress.TotalBytes));
        Assert.Equal(_launcher.ReportedProgress.OrderBy(progress => progress.DownloadedBytes), _launcher.ReportedProgress);
        Assert.Equal(contree.ByteSize, _launcher.ReportedProgress[^1].DownloadedBytes);
    }

    /// @spec store::clears-cache
    [Fact]
    public async Task Clearing_the_cache_forgets_every_downloaded_contree()
    {
        var contree = PublishedContree.Named("Cavabière");
        _launcher.Eternalfest.Publishing(contree);
        await _launcher.Download(contree);

        await _launcher.Store.Clear(TestContext.Current.CancellationToken);

        Assert.Null(await _launcher.FindDownloaded(contree));
        Assert.Empty(await _launcher.Store.ListGames(TestContext.Current.CancellationToken));
        Assert.All(contree.Blobs.Keys, blob => Assert.False(_launcher.Store.HasBlob(blob)));
    }

    private byte[] ReadCached(BlobId id)
    {
        using var stream = _launcher.Store.OpenBlob(id);
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }
}
