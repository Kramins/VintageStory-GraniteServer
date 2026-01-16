using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace GraniteServer.Common;

/// <summary>
/// Local implementation that wraps the in-process ICoreServerAPI.
/// </summary>
public class LocalVintageStoryDataProxy : IVintageStoryDataProxy
{
    private readonly ICoreServerAPI _api;

    public LocalVintageStoryDataProxy(ICoreServerAPI api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    public Task<PlayerSnapshot?> GetPlayerAsync(
        string playerId,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var player = _api.Server.Players?.FirstOrDefault(p =>
            string.Equals(p.PlayerUID, playerId, StringComparison.Ordinal)
        );

        if (player == null)
        {
            return Task.FromResult<PlayerSnapshot?>(null);
        }

        return Task.FromResult<PlayerSnapshot?>(ToSnapshot(player));
    }

    public Task<IReadOnlyList<PlayerSnapshot>> GetOnlinePlayersAsync(
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var players =
            _api.Server.Players?.Select(ToSnapshot).ToList() ?? new List<PlayerSnapshot>();

        return Task.FromResult<IReadOnlyList<PlayerSnapshot>>(players);
    }

    public Task<IReadOnlyList<DetailedPlayerSnapshot>> GetAllPlayersAsync(
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var playerDataManager = (PlayerDataManager)_api.PlayerData;
        var allPlayerData = _api.PlayerData.PlayerDataByUid;
        var allServerPlayers = _api.Server.Players.ToDictionary(p => p.PlayerUID);
        var allBannedPlayers = playerDataManager.BannedPlayers;
        var allWhitelistedPlayers = playerDataManager.WhitelistedPlayers;

        // Build quick lookups keyed by player ID
        var bannedById = allBannedPlayers
            .GroupBy(bp => bp.PlayerUID)
            .ToDictionary(g => g.Key, g => g.First());
        var whitelistedById = allWhitelistedPlayers
            .GroupBy(wp => wp.PlayerUID)
            .ToDictionary(g => g.Key, g => g.First());

        // Union all known player IDs from any source (seen, online, banned, whitelisted)
        var allIds = new HashSet<string>(allPlayerData.Keys);
        allIds.UnionWith(allServerPlayers.Keys);
        allIds.UnionWith(bannedById.Keys);
        allIds.UnionWith(whitelistedById.Keys);

        var fullList = allIds
            .Select(id =>
            {
                allPlayerData.TryGetValue(id, out var pd);
                allServerPlayers.TryGetValue(id, out var sp);
                bannedById.TryGetValue(id, out var bp);
                whitelistedById.TryGetValue(id, out var wp);
                return ToDetailedSnapshot(id, pd, sp, bp, wp);
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<DetailedPlayerSnapshot>>(fullList);
    }

    private static PlayerSnapshot ToSnapshot(IServerPlayer player)
    {
        return new PlayerSnapshot
        {
            Id = player.PlayerUID,
            Name = player.PlayerName,
            RoleCode = player.Role?.Code,
            IsOnline = true,
        };
    }

    private static DetailedPlayerSnapshot ToDetailedSnapshot(
        string playerId,
        IServerPlayerData? pd,
        IServerPlayer? sp,
        PlayerEntry? bp,
        PlayerEntry? wp
    )
    {
        var resolvedName =
            pd?.LastKnownPlayername
            ?? sp?.PlayerName
            ?? bp?.PlayerName
            ?? wp?.PlayerName
            ?? playerId;

        return new DetailedPlayerSnapshot
        {
            Id = playerId,
            Name = resolvedName,
            IpAddress = sp?.IpAddress ?? "Offline",
            LanguageCode = sp?.LanguageCode ?? "N/A",
            ConnectionState = sp?.ConnectionState.ToString() ?? "Offline",
            Ping = sp == null || float.IsNaN(sp.Ping) ? 0 : sp.Ping,
            RolesCode = sp?.Role.Code ?? "N/A",
            FirstJoinDate = pd?.FirstJoinDate ?? string.Empty,
            LastJoinDate = pd?.LastJoinDate ?? string.Empty,
            Privileges = sp?.Privileges.ToArray() ?? Array.Empty<string>(),
            IsAdmin = false,
            IsBanned = bp != null,
            BanReason = bp?.Reason,
            BanBy = bp?.IssuedByPlayerName,
            BanUntil = bp?.UntilDate,
            IsWhitelisted = wp != null,
            WhitelistedReason = wp?.Reason,
            WhitelistedBy = wp?.IssuedByPlayerName,
            WhitelistedUntil = wp?.UntilDate,
        };
    }
}
