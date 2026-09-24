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

        main.Library.OpenCommand.Execute(main.Library.Contrees[0]);
        var page = Assert.IsType<ContreePageViewModel>(main.CurrentPage);
        await page.Load(TestContext.Current.CancellationToken);

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

    [Fact]
    public async Task Goes_back_to_the_library()
    {
        var contree = PublishedContree.Named("Accumulation");
        _launcher.Eternalfest.Publishing(contree);
        var main = _launcher.MainWindow();
        await main.Library.Load(TestContext.Current.CancellationToken);
        main.Library.OpenCommand.Execute(main.Library.Contrees[0]);

        ((ContreePageViewModel)main.CurrentPage).BackCommand.Execute(null);

        Assert.Same(main.Library, main.CurrentPage);
    }

    private async Task<ContreePageViewModel> OpenPage(PublishedContree contree)
    {
        _launcher.Eternalfest.Publishing(contree);
        var main = _launcher.MainWindow();
        await main.Library.Load(TestContext.Current.CancellationToken);
        var page = new ContreePageViewModel(main.Library.Contrees.Single(), _launcher.Catalog, _launcher.Store, _launcher.DownloadGame, _launcher.PlayGame, new Version(5, 1, 2), () => { });
        await page.Load(TestContext.Current.CancellationToken);
        return page;
    }
}
