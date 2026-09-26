using EternalfestDesktop.Application;
using Microsoft.Extensions.Logging;

namespace EternalfestDesktop.Infrastructure.LocalServer;

/// <summary>Plays in the preferred player, or in the fallback when the app doesn't ship the preferred one for this OS.</summary>
/// @spec play::flash-projector-first
public sealed partial class FallbackFlashPlayer(FlashPlayer preferred, FlashPlayer fallback, ILogger<FallbackFlashPlayer> logger) : FlashPlayer
{
    public async Task Play(FlashGame game, CancellationToken cancellationToken)
    {
        try
        {
            await preferred.Play(game, cancellationToken);
        }
        catch (FlashPlayerMissingException missing)
        {
            LogFallingBack(missing.Message);
            await fallback.Play(game, cancellationToken);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Playing in the fallback Flash player: {Reason}")]
    private partial void LogFallingBack(string reason);
}
