namespace Granite.Common.Services;

/// <summary>
/// Service for resolving player names to UIDs and vice versa using the Vintage Story auth server.
/// </summary>
public interface IPlayerNameResolver
{
    /// <summary>
    /// Resolves a player name to their unique player UID.
    /// </summary>
    /// <param name="playerName">The player's name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The player's UID, or null if not found</returns>
    Task<string?> ResolvePlayerNameAsync(string playerName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves a player UID to their current player name.
    /// </summary>
    /// <param name="playerUid">The player's UID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The player's name, or null if not found</returns>
    Task<string?> ResolvePlayerUidAsync(string playerUid, CancellationToken cancellationToken = default);
}
