using EternalfestDesktop.Domain;

namespace EternalfestDesktop.Application;

/// <summary>The quests eternalfest.net applies to a logged-in player, by contrée key.</summary>
public interface QuestBook
{
    /// <summary>Contrées eternalfest.net knows no quest for have <see cref="Progression.None" />.</summary>
    Progression ProgressionOf(string? gameKey);
}
