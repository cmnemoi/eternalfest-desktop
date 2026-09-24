using EternalfestDesktop.Application;
using EternalfestDesktop.Domain;

namespace EternalfestDesktop.Tests.Support;

/// <summary>Stands for Ruffle: records what it was asked to play, and plays like the loader would.</summary>
internal sealed class FakeFlashPlayer : FlashPlayer
{
    private TaskCompletionSource? _playing;

    public List<FlashGame> Played { get; } = [];
    public List<byte[]> LoadersFetched { get; } = [];
    public bool Crashes { get; set; }

    /// <summary>The game window stays open until <see cref="CloseWindow" />.</summary>
    public void KeepWindowOpen() => _playing = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

    public void CloseWindow() => _playing?.TrySetResult();

    public async Task Play(FlashGame game, CancellationToken cancellationToken)
    {
        Played.Add(game);
        using (var http = new HttpClient { BaseAddress = game.Origin })
            LoadersFetched.Add(await http.GetByteArrayAsync("/assets/loader.swf", cancellationToken));
        if (Crashes)
            throw new InvalidOperationException("Ruffle crashed");
        if (_playing is not null)
            await _playing.Task.WaitAsync(cancellationToken);
    }
}
