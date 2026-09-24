using EternalfestDesktop.Tests.Support;
using EternalfestDesktop.Ui.ViewModels;

namespace EternalfestDesktop.Tests.Ui;

public sealed class PlayAgainTest : IDisposable
{
    private readonly TestLauncher _launcher = new();

    public void Dispose() => _launcher.Dispose();

    /// @spec ui::play-again
    [Fact]
    public async Task Isnt_offered_before_any_game()
    {
        _launcher.Eternalfest.Publishing(PublishedContree.Named("Dojo"));

        var main = await Start();

        Assert.Null(main.Library.LastPlayed);
        Assert.False(main.Library.PlayAgainCommand.CanExecute(null));
    }

    /// @spec ui::play-again
    [Fact]
    public async Task Replays_the_last_contree_with_the_same_choices()
    {
        var dojo = PublishedContree.Named("Dojo");
        _launcher.Eternalfest.Publishing(dojo).Publishing(PublishedContree.Named("Himmelen"));
        var main = await Start();
        var page = await Open(main, "Dojo");
        page.SelectedMode!.Options.Single(option => option.Key == "mirror").IsChecked = true;
        await page.PlayCommand.ExecuteAsync(null);
        _launcher.Restart();

        main = await Start();
        Assert.Equal("Dojo", main.Library.LastPlayed?.DisplayName);
        main.Library.PlayAgainCommand.Execute(null);
        await WaitUntil(() => _launcher.FlashPlayer.Played.Count == 2);

        var replayed = _launcher.FlashPlayer.Played[1];
        Assert.Equal(dojo.Id, replayed.Game.Id);
        Assert.Equal(["mirror"], replayed.Run.Options);
    }

    /// @spec ui::play-again
    [Fact]
    public async Task Isnt_offered_once_the_last_contree_left_the_cache()
    {
        _launcher.Eternalfest.Publishing(PublishedContree.Named("Dojo"));
        var main = await Start();
        await (await Open(main, "Dojo")).PlayCommand.ExecuteAsync(null);
        await _launcher.Store.Clear(TestContext.Current.CancellationToken);

        main = await Start();

        Assert.Null(main.Library.LastPlayed);
    }

    private async Task<MainWindowViewModel> Start()
    {
        var main = _launcher.MainWindow();
        await main.Library.Load(TestContext.Current.CancellationToken);
        return main;
    }

    private static async Task<ContreePageViewModel> Open(MainWindowViewModel main, string name)
    {
        main.Library.OpenCommand.Execute(main.Library.Contrees.Single(contree => contree.DisplayName == name));
        var page = (ContreePageViewModel)main.CurrentPage;
        await WaitUntil(() => page.IsLoaded);
        return page;
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!condition())
            await Task.Delay(10, timeout.Token);
    }
}
