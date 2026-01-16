using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GraniteServer.Common;

/// <summary>
/// Read-only proxy abstraction for retrieving data from a Vintage Story server.
/// </summary>
public interface IVintageStoryDataProxy
{
    Task<PlayerSnapshot?> GetPlayerAsync(
        string playerId,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<PlayerSnapshot>> GetOnlinePlayersAsync(
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<DetailedPlayerSnapshot>> GetAllPlayersAsync(
        CancellationToken cancellationToken = default
    );
}
