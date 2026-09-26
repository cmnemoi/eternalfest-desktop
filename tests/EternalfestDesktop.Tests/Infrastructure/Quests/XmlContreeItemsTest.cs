using System.Text;
using EternalfestDesktop.Domain;
using EternalfestDesktop.Infrastructure.FileSystem;
using EternalfestDesktop.Infrastructure.Quests;
using EternalfestDesktop.Tests.Domain;
using EternalfestDesktop.Tests.Support;
using Microsoft.Extensions.Logging;

namespace EternalfestDesktop.Tests.Infrastructure.Quests;

public sealed class XmlContreeItemsTest : IDisposable
{
    private readonly DirectoryInfo _cache = Directory.CreateTempSubdirectory("eternalfest-desktop-tests-");
    private readonly FileSystemGameStore _store;
    private readonly CapturingLogger<XmlContreeItems> _log = new();
    private readonly XmlContreeItems _items;

    public XmlContreeItemsTest()
    {
        _store = new FileSystemGameStore(_cache.FullName);
        _items = new XmlContreeItems(_store, _log);
    }

    public void Dispose() => _cache.Delete(recursive: true);

    /// @spec profile::complete-inventory
    [Fact]
    public async Task Lists_each_item_of_the_content_once()
    {
        var build = await WithContent("""
            <?xml version="1.0" encoding="UTF-8"?>
            <game>
              <items type="special"><family id="0"><item id="1" rarity="0"/><item id="1"/><item id="x"/></family></items>
              <items type="score"><family id="1000"><item id="2" value="5"/></family></items>
              <datas><item id="77"/></datas>
            </game>
            """);

        var items = await _items.ListedIn(build, TestContext.Current.CancellationToken);

        Assert.Equal([1, 2], items.Order());
        Assert.Empty(_log.Entries);
    }

    /// @spec profile::complete-inventory
    [Fact]
    public async Task Lists_nothing_for_content_without_items()
    {
        var build = await WithContent("<game><levels/><items type=\"special\"/></game>");

        Assert.Empty(await _items.ListedIn(build, TestContext.Current.CancellationToken));
        Assert.Empty(_log.Entries);
    }

    /// @spec profile::complete-inventory
    [Fact]
    public async Task Lists_nothing_for_a_build_without_content()
    {
        Assert.Empty(await _items.ListedIn(ProgressionTest.Build("0"), TestContext.Current.CancellationToken));
        Assert.Empty(_log.Entries);
    }

    /// @spec profile::unreadable-content
    [Fact]
    public async Task Reports_content_that_isnt_xml()
    {
        var build = await WithContent("<game><items><family id=\"0\"><item id=\"1\"/>");

        Assert.Empty(await _items.ListedIn(build, TestContext.Current.CancellationToken));
        Assert.Equal(LogLevel.Warning, Assert.Single(_log.Entries).Level);
    }

    /// @spec profile::unreadable-content
    [Fact]
    public async Task Reports_content_missing_from_the_cache()
    {
        var build = ProgressionTest.Build("0") with { Content = Content(new BlobId(Guid.NewGuid())) };

        Assert.Empty(await _items.ListedIn(build, TestContext.Current.CancellationToken));
        Assert.Equal(LogLevel.Warning, Assert.Single(_log.Entries).Level);
    }

    private async Task<GameBuild> WithContent(string xml)
    {
        var id = new BlobId(Guid.NewGuid());
        await _store.SaveBlob(id, new MemoryStream(Encoding.UTF8.GetBytes(xml)), TestContext.Current.CancellationToken);
        return ProgressionTest.Build("0") with { Content = Content(id) };
    }

    private static Blob Content(BlobId id) => new(id, "application/xml", 0, "");
}
