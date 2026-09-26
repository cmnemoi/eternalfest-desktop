using System.Text.Json.Nodes;
using EternalfestDesktop.Application;
using EternalfestDesktop.Domain;

namespace EternalfestDesktop.Tests.Support;

/// <summary>Stands for Ruffle: records what it was asked to play, and plays like the loader would.</summary>
internal sealed class FakeFlashPlayer : FlashPlayer
{
    private TaskCompletionSource? _playing;
    private (string Key, string Value)[]? _result;

    public List<FlashGame> Played { get; } = [];
    public List<byte[]> LoadersFetched { get; } = [];

    /// <summary>The contrée the backend gave the loader, for each game.</summary>
    public List<JsonNode> GamesServed { get; } = [];

    /// <summary>What the backend answered when the loader started each run.</summary>
    public List<JsonNode> RunsStarted { get; } = [];
    public bool Crashes { get; set; }

    /// <summary>Whether the launcher closed the game window, rather than the player.</summary>
    public bool WindowClosedByLauncher { get; private set; }

    /// <summary>The loader reports a defeat, then the game window stays open until the launcher closes it.</summary>
    public void LosesAtLevel(int level, params int[] scores) =>
        _result = [("is_victory", "false"), ("max_level", $"{level}"), ("scores", $"[{string.Join(',', scores)}]"), ("items", "{}"), ("stats", "{}")];

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
            if (_result is not null)
                await EndTheGame(http, game, _result, cancellationToken);
        }
        if (Crashes)
            throw new InvalidOperationException("Ruffle crashed");
        if (_playing is not null)
            await _playing.Task.WaitAsync(cancellationToken);
    }

    /// <summary>The launcher may close the window as soon as the loader has its answer, even before it reads it.</summary>
    private async Task EndTheGame(HttpClient http, FlashGame game, (string Key, string Value)[] result, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await http.PostAsync($"/api/v1/runs/{game.Run.Id}/result", new FormUrlEncodedContent([new("flash", "true"), .. result.Select(field => new KeyValuePair<string, string>(field.Key, field.Value))]), cancellationToken);
            response.EnsureSuccessStatusCode();
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            WindowClosedByLauncher = true;
            throw;
        }
    }
}
