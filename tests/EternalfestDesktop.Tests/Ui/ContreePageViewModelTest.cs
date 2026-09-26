using EternalfestDesktop.Domain;
using EternalfestDesktop.Tests.Support;
using EternalfestDesktop.Ui.Resources;
using EternalfestDesktop.Ui.ViewModels;

namespace EternalfestDesktop.Tests.Ui;

public sealed class ContreePageViewModelTest : IDisposable
{
    private readonly TestLauncher _launcher = new();

    public void Dispose() => _launcher.Dispose();

    /// @spec ui::contree-page
    [Fact]
    public async Task Offers_the_visible_modes_and_their_visible_options_all_enabled()
    {
        var page = await OpenPage(PublishedContree.Named("Accumulation"));

        Assert.True(page.IsLoaded);
        Assert.Equal(["solo", "multicoop"], page.Modes.Select(mode => mode.Key));
        Assert.Equal("solo", page.SelectedMode!.Key);
        Assert.Equal(["mirror", "ninja"], page.SelectedMode.Options.Select(option => option.Key));
        Assert.Equal(["fr-FR"], page.Locales.Select(locale => locale.Code));
        Assert.Null(page.LoaderWarning);
    }

    /// @spec ui::contree-page
    /// @spec play::launches-ruffle
    [Fact]
    public async Task Plays_with_the_chosen_mode_options_volume_and_fullscreen()
    {
        var page = await OpenPage(PublishedContree.Named("Accumulation"));
        page.SelectedMode!.Options.Single(option => option.Key == "ninja").IsChecked = true;
        page.Volume = 35;
        page.Fullscreen = true;

        await page.PlayCommand.ExecuteAsync(null);

        var played = Assert.Single(_launcher.FlashPlayer.Played);
        Assert.Equal("solo", played.Run.Mode);
        Assert.Equal(["ninja"], played.Run.Options);
        Assert.Equal(35, played.Run.Settings.Volume);
        Assert.True(played.Fullscreen);
        Assert.Null(page.ErrorMessage);
        Assert.Null(page.Status);
        Assert.True(page.Contree.IsDownloaded);
    }

    /// @spec ui::download-progress
    [Fact]
    public async Task Explains_a_failed_download_in_plain_words()
    {
        var contree = PublishedContree.Named("Accumulation");
        var page = await OpenPage(contree);
        _launcher.Eternalfest.DropsConnectionAtBlob = contree.BlobOf("content");

        await page.PlayCommand.ExecuteAsync(null);

        Assert.Equal(Strings.ErrorUnreachable, page.ErrorMessage);
        Assert.Empty(_launcher.FlashPlayer.Played);
        Assert.False(page.Contree.IsDownloaded);
    }

    /// @spec ui::download-progress
    [Fact]
    public async Task Explains_a_crash_of_the_game()
    {
        var page = await OpenPage(PublishedContree.Named("Accumulation"));
        _launcher.FlashPlayer.Crashes = true;

        await page.PlayCommand.ExecuteAsync(null);

        Assert.NotNull(page.ErrorMessage);
        Assert.Null(page.Status);
    }

    /// @spec ui::contree-page
    [Fact]
    public async Task Cant_open_a_contree_never_downloaded_without_network()
    {
        var contree = PublishedContree.Named("Accumulation");
        _launcher.Eternalfest.Publishing(contree);
        var main = _launcher.MainWindow();
        await main.Library.Load(TestContext.Current.CancellationToken);
        _launcher.Eternalfest.IsUnreachable = true;

        await main.Library.OpenCommand.ExecuteAsync(main.Library.Contrees[0]);
        var page = Assert.IsType<ContreePageViewModel>(main.CurrentPage);

        Assert.False(page.IsLoaded);
        Assert.Equal(Strings.ErrorUnreachable, page.ErrorMessage);
    }

    /// @spec play::warns-newer-loader
    [Fact]
    public async Task Warns_when_the_contree_expects_a_newer_loader()
    {
        var page = await OpenPage(PublishedContree.Named("Accumulation").RequiringLoader("6.0.0"));

        Assert.NotNull(page.LoaderWarning);
        Assert.Contains("6.0.0", page.LoaderWarning, StringComparison.Ordinal);
        Assert.True(page.PlayCommand.CanExecute(null));
    }

    /// @spec ui::contree-page
    [Fact]
    public async Task Preselects_the_choices_of_the_last_game_of_the_contree()
    {
        var contree = PublishedContree.Named("Accumulation");
        var page = await OpenPage(contree);
        page.SelectedMode = page.Modes.Single(mode => mode.Key == "multicoop");
        page.SelectedMode.Options.Single().IsChecked = true;
        page.Volume = 20;
        await page.PlayCommand.ExecuteAsync(null);
        _launcher.Restart();

        var reopened = await OpenPage(contree);

        Assert.Equal("multicoop", reopened.SelectedMode!.Key);
        Assert.True(reopened.SelectedMode.Options.Single(option => option.Key == "lifesharing").IsChecked);
        Assert.Equal(20, reopened.Volume);
    }

    /// @spec ui::contree-page
    [Fact]
    public async Task Ignores_remembered_choices_the_contree_no_longer_offers()
    {
        var contree = PublishedContree.Named("Accumulation");
        var page = await OpenPage(contree);
        page.SelectedMode = page.Modes.Single(mode => mode.Key == "multicoop");
        await page.PlayCommand.ExecuteAsync(null);
        contree.Modes.Remove("multicoop");
        _launcher.Eternalfest.Publishing(contree.InVersion("2.0.0"));
        await _launcher.DownloadGame.Update(contree.Id, null, TestContext.Current.CancellationToken);

        var reopened = await OpenPage(contree);

        Assert.Equal("solo", reopened.SelectedMode!.Key);
        await reopened.PlayCommand.ExecuteAsync(null);
        Assert.Null(reopened.ErrorMessage);
    }

    /// @spec ui::profile-picker
    [Fact]
    public async Task Selects_the_complete_profile_for_a_contree_never_played()
    {
        var page = await OpenPage(Cavernes());

        Assert.Equal(PlayerProfile.Complete, page.Profile);
        Assert.True(page.IsCompleteProfile);
        Assert.False(page.IsNewPlayer);
    }

    /// @spec ui::profile-picker
    [Fact]
    public async Task Offers_the_modes_and_options_the_selected_profile_unlocks()
    {
        var page = await OpenPage(Cavernes());

        Assert.Equal(["solo", "multicoop", "deluxe"], page.Modes.Select(mode => mode.Key));
        Assert.Equal(["mirror", "ninja", "insight"], page.SelectedMode!.Options.Select(option => option.Key));

        page.IsNewPlayer = true;

        Assert.Equal(PlayerProfile.NewPlayer, page.Profile);
        Assert.Equal(["solo", "multicoop"], page.Modes.Select(mode => mode.Key));
        Assert.Equal(["mirror", "ninja"], page.SelectedMode!.Options.Select(option => option.Key));
    }

    /// @spec ui::profile-picker
    [Fact]
    public async Task Keeps_the_mode_and_options_still_offered_when_switching_profiles()
    {
        var page = await OpenPage(Cavernes());
        Check(page, "mirror", "insight");

        page.IsNewPlayer = true;

        Assert.Equal("solo", page.SelectedMode!.Key);
        Assert.Equal(["mirror"], page.SelectedMode.Options.Where(option => option.IsChecked).Select(option => option.Key));
    }

    /// @spec ui::profile-picker
    [Fact]
    public async Task Falls_back_to_the_first_mode_when_the_selected_one_is_no_longer_offered()
    {
        var page = await OpenPage(Cavernes());
        page.SelectedMode = page.Modes.Single(mode => mode.Key == "deluxe");
        Check(page, "mirror", "insight");

        page.IsNewPlayer = true;

        Assert.Equal("solo", page.SelectedMode!.Key);
    }

    /// @spec ui::profile-picker
    /// @spec profile::new-player-as-published
    [Fact]
    public async Task Plays_and_remembers_the_selected_profile()
    {
        var contree = Cavernes();
        var page = await OpenPage(contree);
        page.IsNewPlayer = true;
        await page.PlayCommand.ExecuteAsync(null);
        _launcher.Restart();

        var reopened = await OpenPage(contree);

        Assert.Empty(Assert.Single(_launcher.FlashPlayer.RunsStarted)["items"]!.AsObject());
        Assert.Equal(PlayerProfile.NewPlayer, reopened.Profile);
    }

    /// @spec ui::profile-picker
    [Fact]
    public async Task Plays_the_complete_profile_selected_by_default()
    {
        var page = await OpenPage(Cavernes());
        Check(page, "insight");

        await page.PlayCommand.ExecuteAsync(null);

        Assert.Null(page.ErrorMessage);
        Assert.Equal(["insight"], Assert.Single(_launcher.FlashPlayer.Played).Run.Options);
        Assert.NotEmpty(Assert.Single(_launcher.FlashPlayer.RunsStarted)["items"]!.AsObject());
    }

    [Fact]
    public async Task Goes_back_to_the_library()
    {
        var contree = PublishedContree.Named("Accumulation");
        _launcher.Eternalfest.Publishing(contree);
        var main = _launcher.MainWindow();
        await main.Library.Load(TestContext.Current.CancellationToken);
        await main.Library.OpenCommand.ExecuteAsync(main.Library.Contrees[0]);

        await ((ContreePageViewModel)main.CurrentPage).BackCommand.ExecuteAsync(null);

        Assert.Same(main.Library, main.CurrentPage);
    }

    /// <summary>A contrée keyed like "Les Cavernes de Hammerfest": the carrot quest unlocks <c>insight</c> and <c>deluxe</c>.</summary>
    private static PublishedContree Cavernes() =>
        PublishedContree.Named("Hammerfest").WithOption("solo", "insight", visible: false).WithMode("deluxe", visible: false, "mirror", "insight");

    private static void Check(ContreePageViewModel page, params string[] options)
    {
        foreach (var option in page.SelectedMode!.Options)
            option.IsChecked = options.Contains(option.Key);
    }

    private async Task<ContreePageViewModel> OpenPage(PublishedContree contree)
    {
        _launcher.Eternalfest.Publishing(contree);
        var main = _launcher.MainWindow();
        await main.Library.Load(TestContext.Current.CancellationToken);
        var page = _launcher.ContreePage(main.Library.Contrees.Single(), () => Task.CompletedTask);
        await page.Load(TestContext.Current.CancellationToken);
        return page;
    }
}
