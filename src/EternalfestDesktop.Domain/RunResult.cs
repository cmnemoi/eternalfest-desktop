namespace EternalfestDesktop.Domain;

/// <summary>How a run ended, as the loader reports it. Offline, it is shown once and never kept.</summary>
/// <param name="Scores">One per player: two in multicoop.</param>
public sealed record RunResult(bool IsVictory, int HighestLevel, IReadOnlyList<int> Scores);
