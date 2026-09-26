using EternalfestDesktop.Domain;

namespace EternalfestDesktop.Application;

/// <summary>The items a downloaded contrée defines in its content.</summary>
public interface ContreeItems
{
    /// <summary>Each item id once. Content that can't be read lists nothing, and the problem is logged.</summary>
    Task<IReadOnlyList<int>> ListedIn(GameBuild build, CancellationToken cancellationToken);
}
