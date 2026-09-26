using System.Text.Json.Nodes;
using EternalfestDesktop.Application;
using EternalfestDesktop.Domain;

namespace EternalfestDesktop.Tests.Support;

/// <summary>Stands for Ruffle: records what it was asked to play, and plays like the loader would.</summary>
internal sealed class FakeFlashPlayer : FlashPlayer
{
    private TaskCompletionSource? _playing;

    public List<FlashGame> Played { get; } = [];
    public List<byte[]> LoadersFetched { get; } = [];

    /// <summary>The contrée the backend gave the loader, for each game.</summary>
    public List<JsonNode> GamesServed { get; } = [];

    /// <summary>What the backend answered when the loader started each run.</summary>
    public List<JsonNode> RunsStarted { get; } = [];
    public bool Crashes { get; set; }

    /// <summary>The game window stays open until <see cref="CloseWindow" />.</summary>
    public void KeepWindowOpen() => _playing = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

    public void CloseWindow() => _playing?.TrySetResult();

    public async Task Play(FlashGame game, CancellationToken cancellationToken)
    {
        Played.Add(game);
        using (var http = new HttpClient { BaseAddress = game.Origin })
        {
            LoadersFetched.Add(await http.GetByteArrayAsync("/assets/loader.swf", cancellationToken));
            GamesServed.Add(JsonNode.Parse(await http.GetStringAsync($"/api/v1/games/{game.Game.Id}", cancellationToken))!);
            using var start = await http.PostAsync($"/api/v1/runs/{game.Run.Id}/start", new FormUrlEncodedContent([new("flash", "true"), new("key", "0000")]), cancellationToken);
            RunsStarted.Add(JsonNode.Parse(await start.EnsureSuccessStatusCode().Content.ReadAsStringAsync(cancellationToken))!);
        }
        if (Crashes)
            throw new InvalidOperationException("Ruffle crashed");
        if (_playing is not null)
            await _playing.Task.WaitAsync(cancellationToken);
    }
}
