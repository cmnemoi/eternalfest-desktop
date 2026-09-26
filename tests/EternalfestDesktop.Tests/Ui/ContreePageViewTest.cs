using EternalfestDesktop.Tests.Support;
using EternalfestDesktop.Ui.Resources;
using EternalfestDesktop.Ui.ViewModels;

namespace EternalfestDesktop.Tests.Ui;

public sealed class ContreePageViewTest : IDisposable
{
    private readonly TestLauncher _launcher = new();

    public void Dispose() => _launcher.Dispose();

    /// @spec ui::contree-page
    [Fact]
    public Task Shows_the_contree_and_the_visible_options_of_its_first_mode() => HeadlessApp.Run(async () =>
    {
        var window = await OpenPage(PublishedContree.Named("Accumulation").InVersion("1.2.3"));

        Assert.True(window.Shows("Accumulation"));
        Assert.True(window.Shows(Text.Format(Strings.Version, "1.2.3")));
        Assert.True(window.Shows("Description of Accumulation"));
        Assert.True(window.Shows("Aventure"));
        Assert.True(window.Shows("Miroir"));
        Assert.True(window.Shows("Ninjutsu"));
        Assert.False(window.Shows("Debug"));
        Assert.False(window.Shows("Partage de vies"));
    });

    /// @spec ui::contree-page
    [Fact]
    public Task Shows_nothing_that_only_applies_while_downloading_updating_or_after_a_game() => HeadlessApp.Run(async () =>
    {
        var window = await OpenPage(PublishedContree.Named("Accumulation"));

        Assert.DoesNotContain(Strings.Cancel, window.Buttons);
        Assert.DoesNotContain("", window.Buttons);
        Assert.False(window.Shows(Strings.UpdateAvailable));
        Assert.False(window.Shows(Strings.GameOver));
        Assert.False(window.Shows(Strings.ScoresNotSaved));
    });

    /// @spec ui::contree-page
    /// @spec play::launches-ruffle
    [Fact]
    public Task Plays_with_the_options_checked_with_the_mouse() => HeadlessApp.Run(async () =>
    {
        var window = await OpenPage(PublishedContree.Named("Accumulation"));

        window.Click("Ninjutsu");
        window.Click(Strings.Fullscreen);
        window.Click(Strings.Play);

        await window.WaitUntil(() => _launcher.FlashPlayer.Played.Count == 1, "the game to be played");
        var played = _launcher.FlashPlayer.Played[0];
        Assert.Equal(["ninja"], played.Run.Options);
        Assert.True(played.Fullscreen);
    });

    /// @spec ui::contree-page
    [Fact]
    public Task Unchecking_an_option_plays_without_it() => HeadlessApp.Run(async () =>
    {
        var window = await OpenPage(PublishedContree.Named("Accumulation"));
        window.Click("Ninjutsu");

        window.Click("Ninjutsu");
        window.Click(Strings.Play);

        await window.WaitUntil(() => _launcher.FlashPlayer.Played.Count == 1, "the game to be played");
        Assert.Empty(_launcher.FlashPlayer.Played[0].Run.Options);
    });

    /// @spec ui::contree-page
    [Fact]
    public Task Choosing_another_mode_shows_its_options_and_plays_it() => HeadlessApp.Run(async () =>
    {
        var window = await OpenPage(PublishedContree.Named("Accumulation"));

        window.Choose("Multi Coopératif");

        Assert.True(window.Shows("Partage de vies"));
        Assert.False(window.Shows("Ninjutsu"));
        window.Click(Strings.Play);
        await window.WaitUntil(() => _launcher.FlashPlayer.Played.Count == 1, "the game to be played");
        Assert.Equal("multicoop", _launcher.FlashPlayer.Played[0].Run.Mode);
    });

    /// @spec ui::contree-page
    [Fact]
    public Task Says_when_the_chosen_mode_has_no_option() => HeadlessApp.Run(async () =>
    {
        var window = await OpenPage(PublishedContree.Named("Accumulation").WithMode("Tutoriel", visible: true));
        Assert.False(window.Shows(Strings.NoOption));

        window.Choose("Tutoriel");

        Assert.True(window.Shows(Strings.NoOption));
    });

    /// @spec ui::profile-picker
    [Fact]
    public Task Plays_as_a_new_player_once_picked() => HeadlessApp.Run(async () =>
    {
        var window = await OpenPage(PublishedContree.Named("Accumulation"));
        Assert.True(window.IsChecked(Strings.CompleteProfile));

        window.Click(Strings.NewPlayer);
        window.Click(Strings.Play);

        await window.WaitUntil(() => _launcher.FlashPlayer.RunsStarted.Count == 1, "the run to start");
        Assert.False(window.IsChecked(Strings.CompleteProfile));
        Assert.Empty(_launcher.FlashPlayer.RunsStarted[0]["items"]!.AsObject());
    });

    /// @spec ui::profile-picker
    [Fact]
    public Task Reopens_a_contree_with_the_profile_it_was_played_with() => HeadlessApp.Run(async () =>
    {
        var window = await OpenPage(PublishedContree.Named("Accumulation"));
        window.Click(Strings.NewPlayer);
        window.Click(Strings.Play);
        await window.WaitUntil(() => _launcher.FlashPlayer.Played.Count == 1, "the game to be played");
        window.Click(Strings.Back);
        await window.WaitUntil(() => window.Cards.Count == 1, "the library");

        window.Click("Accumulation");
        await window.WaitFor(Strings.Play);

        Assert.True(window.IsChecked(Strings.NewPlayer));
        Assert.False(window.IsChecked(Strings.CompleteProfile));
    });

    /// @spec ui::contree-page
    [Fact]
    public Task Explains_why_a_contree_never_downloaded_cant_open_without_network() => HeadlessApp.Run(async () =>
    {
        _launcher.Eternalfest.Publishing(PublishedContree.Named("Accumulation"));
        var window = await OpenLibrary();
        _launcher.Eternalfest.IsUnreachable = true;

        window.Click("Accumulation");

        await window.WaitFor(Strings.ErrorUnreachable);
        Assert.DoesNotContain(Strings.Play, window.Buttons);
    });

    /// @spec play::warns-newer-loader
    [Fact]
    public Task Warns_when_the_contree_expects_a_newer_loader() => HeadlessApp.Run(async () =>
    {
        var window = await OpenPage(PublishedContree.Named("Accumulation").RequiringLoader("99.0.0"));

        Assert.Contains(window.Texts, text => text.Contains("99.0.0", StringComparison.Ordinal));
        Assert.Contains(Strings.Play, window.Buttons);
    });

    /// @spec store::detects-newer-build
    [Fact]
    public Task Updates_an_outdated_contree_from_its_page() => HeadlessApp.Run(async () =>
    {
        var contree = PublishedContree.Named("Accumulation").InVersion("1.0.0");
        _launcher.Eternalfest.Publishing(contree);
        await _launcher.Download(contree);
        _launcher.Eternalfest.Publishing(contree.InVersion("2.0.0"));
        var window = await OpenLibrary();
        window.Click("Accumulation");
        await window.WaitFor(Strings.UpdateAvailable);

        window.Click(Text.Format(Strings.Update, "2.0.0"));

        await window.WaitUntilGone(Strings.UpdateAvailable);
        Assert.Equal("2.0.0", (await _launcher.FindDownloaded(contree))!.Build.Version);
    });

    /// @spec ui::game-summary
    [Fact]
    public Task Sums_up_the_game_once_it_ends() => HeadlessApp.Run(async () =>
    {
        var window = await OpenPage(PublishedContree.Named("Accumulation"));
        _launcher.FlashPlayer.LosesAtLevel(11, 12345, 678);

        window.Click(Strings.Play);

        await window.WaitFor(Strings.GameOver);
        Assert.True(window.Shows(Text.Format(Strings.HighestLevel, 11)));
        Assert.Equal(2, window.Texts.Count(text => text.Contains("12", StringComparison.Ordinal) && text.Contains("345", StringComparison.Ordinal)
            || text.Contains("678", StringComparison.Ordinal)));
        Assert.True(window.Shows(Strings.ScoresNotSaved));
    });

    /// @spec ui::game-summary
    [Fact]
    public Task The_game_summary_covers_the_page_until_closed() => HeadlessApp.Run(async () =>
    {
        var window = await OpenPage(PublishedContree.Named("Accumulation"));
        _launcher.FlashPlayer.LosesAtLevel(11, 12345);
        window.Click(Strings.Play);
        await window.WaitFor(Strings.GameOver);

        window.Click(Strings.Back);
        Assert.True(window.Shows(Strings.GameOver));

        window.Click(Strings.Close);
        await window.WaitUntilGone(Strings.GameOver);
        window.Click(Strings.Back);
        await window.WaitUntil(() => window.Cards.Count == 1, "the library");
    });

    /// @spec ui::game-summary
    [Fact]
    public Task Replays_from_the_game_summary() => HeadlessApp.Run(async () =>
    {
        var window = await OpenPage(PublishedContree.Named("Accumulation"));
        _launcher.FlashPlayer.LosesAtLevel(11, 12345);
        window.Click(Strings.Play);
        await window.WaitFor(Strings.GameOver);

        window.Click(Strings.Replay);

        await window.WaitUntil(() => _launcher.FlashPlayer.Played.Count == 2, "the game to be played again");
    });

    private async Task<LauncherWindow> OpenPage(PublishedContree contree)
    {
        _launcher.Eternalfest.Publishing(contree);
        var window = await OpenLibrary();
        window.Click(contree.Name);
        await window.WaitFor(Strings.Play);
        return window;
    }

    private async Task<LauncherWindow> OpenLibrary()
    {
        var window = LauncherWindow.Opening(_launcher.MainWindow());
        await window.WaitUntil(() => !window.Main.Library.IsLoading && window.Buttons.Contains(Strings.Settings), "the library to load");
        return window;
    }
}
