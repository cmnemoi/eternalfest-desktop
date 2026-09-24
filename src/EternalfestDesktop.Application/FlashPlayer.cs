using EternalfestDesktop.Domain;

namespace EternalfestDesktop.Application;

/// <summary>Plays the Eternalfest loader, in its own window.</summary>
public interface FlashPlayer
{
    /// <summary>Returns once the game window is closed.</summary>
    /// <exception cref="FlashPlayerMissingException" />
    Task Play(FlashGame game, CancellationToken cancellationToken);
}

/// <param name="Origin">Where the offline backend serves the loader, the API and the blobs.</param>
public sealed record FlashGame(Uri Origin, Game Game, Run Run, bool Fullscreen);
