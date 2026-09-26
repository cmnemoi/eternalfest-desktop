using EternalfestDesktop.Tests.Support;
using EternalfestDesktop.Ui.Resources;
using EternalfestDesktop.Ui.ViewModels;

namespace EternalfestDesktop.Tests.Ui;

public sealed class LibraryViewTest : IDisposable
{
    private readonly TestLauncher _launcher = new();

    public void Dispose() => _launcher.Dispose();

    /// @spec ui::library
    [Fact]
    public Task Shows_a_card_for_each_contree_of_the_catalog() => HeadlessApp.Run(async () =>
    {
        Publish("Dojo", "Himmelen");

        var window = await OpenLibrary();

        Assert.Equal(["Dojo", "Himmelen"], window.Cards);
        Assert.Equal(["Dojo", "Description of Dojo"], window.TextsOnCard("Dojo"));
        Assert.False(window.Shows(Strings.OfflineNotice));
        Assert.False(window.Shows(Strings.Loading));
    });

    /// @spec catalog::first-launch-without-network
    [Fact]
    public Task Says_it_is_offline_on_a_fresh_install_without_network() => HeadlessApp.Run(async () =>
    {
        _launcher.Eternalfest.IsUnreachable = true;

        var window = LauncherWindow.Opening(_launcher.MainWindow());

        await window.WaitFor(Strings.OfflineNotice);
        Assert.Empty(window.Cards);
        Assert.False(window.Shows(Strings.Loading));
    });

    /// @spec catalog::last-known-catalog-offline
    [Fact]
    public Task Retrying_once_back_online_lists_the_catalog_again() => HeadlessApp.Run(async () =>
    {
        Publish("Dojo");
        _launcher.Eternalfest.IsUnreachable = true;
        var window = LauncherWindow.Opening(_launcher.MainWindow());
        await window.WaitFor(Strings.OfflineNotice);
        _launcher.Eternalfest.IsUnreachable = false;

        window.Click(Strings.Retry);

        await window.WaitUntilGone(Strings.OfflineNotice);
        Assert.Equal(["Dojo"], window.Cards);
    });

    /// @spec ui::library
    [Fact]
    public Task Typing_a_search_keeps_only_the_matching_cards() => HeadlessApp.Run(async () =>
    {
        Publish("Les Cavernes de Hammerfest", "Dojo", "Himmelen");
        var window = await OpenLibrary();

        window.Type(Strings.SearchPlaceholder, "cavernes");

        Assert.Equal(["Les Cavernes de Hammerfest"], window.Cards);
    });

    /// @spec ui::library
    [Fact]
    public Task Says_when_no_contree_matches_the_search() => HeadlessApp.Run(async () =>
    {
        Publish("Dojo");
        var window = await OpenLibrary();

        window.Type(Strings.SearchPlaceholder, "zzz");

        Assert.Empty(window.Cards);
        Assert.True(window.Shows(Text.Format(Strings.NoMatch, "zzz")));
    });

    /// @spec ui::library
    /// @spec store::detects-newer-build
    [Fact]
    public Task Badges_only_the_downloaded_and_outdated_contrees() => HeadlessApp.Run(async () =>
    {
        var contrees = Publish("Dojo", "Himmelen", "Trolilol");
        var himmelen = contrees[1].InVersion("1.0.0");
        _launcher.Eternalfest.Publishing(himmelen);
        await _launcher.Download(contrees[0]);
        await _launcher.Download(himmelen);
        _launcher.Eternalfest.Publishing(himmelen.InVersion("2.0.0"));

        var window = await OpenLibrary();

        Assert.Contains(Strings.Downloaded, window.TextsOnCard("Dojo"));
        Assert.DoesNotContain(Strings.UpdateAvailable, window.TextsOnCard("Dojo"));
        Assert.Contains(Strings.UpdateAvailable, window.TextsOnCard("Himmelen"));
        Assert.DoesNotContain(Strings.Downloaded, window.TextsOnCard("Trolilol"));
        Assert.DoesNotContain(Strings.UpdateAvailable, window.TextsOnCard("Trolilol"));
    });

    /// @spec ui::contree-page
    [Fact]
    public Task Clicking_a_card_opens_its_page_and_back_returns_to_the_library() => HeadlessApp.Run(async () =>
    {
        Publish("Dojo", "Himmelen");
        var window = await OpenLibrary();

        window.Click("Himmelen");
        await window.WaitFor(Strings.Play);
        Assert.Empty(window.Cards);
        Assert.True(window.Shows("Description of Himmelen"));

        window.Click(Strings.Back);
        await window.WaitFor("Dojo");
        Assert.Equal(["Dojo", "Himmelen"], window.Cards);
    });

    /// @spec ui::play-again
    [Fact]
    public Task Offers_to_play_the_last_played_contree_again_only_once_one_was_played() => HeadlessApp.Run(async () =>
    {
        Publish("Dojo");
        var window = await OpenLibrary();
        Assert.DoesNotContain("", window.Buttons);
        Assert.False(window.Shows(Text.Format(Strings.PlayAgain, "Dojo")));

        window.Click("Dojo");
        await window.WaitFor(Strings.Play);
        window.Click(Strings.Play);
        await window.WaitUntil(() => _launcher.FlashPlayer.Played.Count == 1, "the game to be played");
        window.Click(Strings.Back);

        await window.WaitFor(Text.Format(Strings.PlayAgain, "Dojo"));
    });

    /// @spec ui::settings
    [Fact]
    public Task Opens_the_settings() => HeadlessApp.Run(async () =>
    {
        var window = await OpenLibrary();

        window.Click(Strings.Settings);

        await window.WaitFor(Strings.ClearCache);
    });

    /// @spec ui::no-first-launch-setup
    [Fact]
    public Task Always_says_the_app_is_unofficial() => HeadlessApp.Run(async () =>
    {
        Publish("Dojo");
        var window = await OpenLibrary();
        Assert.True(window.Shows(Strings.UnofficialNotice));

        window.Click("Dojo");
        await window.WaitFor(Strings.Play);
        Assert.True(window.Shows(Strings.UnofficialNotice));
    });

    private List<PublishedContree> Publish(params string[] names)
    {
        var contrees = names.Select(PublishedContree.Named).ToList();
        foreach (var contree in contrees)
            _launcher.Eternalfest.Publishing(contree);
        return contrees;
    }

    private async Task<LauncherWindow> OpenLibrary()
    {
        var window = LauncherWindow.Opening(_launcher.MainWindow());
        await window.WaitUntil(() => !window.Main.Library.IsLoading && !window.Shows(Strings.Loading), "the library to load");
        return window;
    }
}
