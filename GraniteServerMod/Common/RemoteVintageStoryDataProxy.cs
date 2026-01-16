using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GraniteServer.Common;

/// <summary>
/// Placeholder for a remote implementation. Not implemented yet.
/// </summary>
public class RemoteVintageStoryDataProxy : IVintageStoryDataProxy
{
    public Task<PlayerSnapshot?> GetPlayerAsync(
        string playerId,
        CancellationToken cancellationToken = default
    )
    {
        throw new NotSupportedException("Remote proxy not implemented yet.");
    }

    public Task<IReadOnlyList<PlayerSnapshot>> GetOnlinePlayersAsync(
        CancellationToken cancellationToken = default
    )
    {
        throw new NotSupportedException("Remote proxy not implemented yet.");
    }

    public Task<IReadOnlyList<DetailedPlayerSnapshot>> GetAllPlayersAsync(
        CancellationToken cancellationToken = default
    )
    {
        throw new NotSupportedException("Remote proxy not implemented yet.");
    }
}
