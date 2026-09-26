using EternalfestDesktop.Domain;
using EternalfestDesktop.Ui.Resources;

namespace EternalfestDesktop.Ui.ViewModels;

/// <summary>How the game that just ended went. Offline, it is shown once and never kept.</summary>
/// @spec ui::game-summary
public sealed class GameSummaryViewModel(RunResult result)
{
    public bool IsVictory { get; } = result.IsVictory;
    public string Title { get; } = result.IsVictory ? Strings.Victory : Strings.GameOver;
    public int HighestLevel { get; } = result.HighestLevel;
    public IReadOnlyList<PlayerScore> Scores { get; } = ScoresOf(result.Scores);

    /// <summary>A single player's score is just "the score"; in multicoop, each player is named.</summary>
    private static List<PlayerScore> ScoresOf(IReadOnlyList<int> scores) => scores.Count == 1
        ? [new PlayerScore(Strings.Score, scores[0])]
        : [.. scores.Select((score, index) => new PlayerScore(Text.Format(Strings.PlayerNumber, index + 1), score))];
}

public sealed record PlayerScore(string Player, int Score)
{
    public string Line => Text.Format(Strings.ScoreLine, Player, Score);
}
