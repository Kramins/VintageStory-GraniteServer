using System;
using System.Collections.Generic;
using System.Linq;
using GraniteServer.Api.Models;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace GraniteServer.Api.Services;

public class PlayerService
{
    private readonly ICoreServerAPI _api;
    public PlayerService(ICoreServerAPI api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    public List<PlayerDTO> GetAllPlayers()
    {
        // All players that have ever connected
        var allPlayerData = _api.PlayerData.PlayerDataByUid.Values.ToList();

        // All players that have connected since server start
        var allServerPlayers = _api.Server.Players.ToList();

        // All banned and whitelisted players
        var allBannedPlayers = ((Vintagestory.Server.PlayerDataManager)_api.PlayerData).BannedPlayers;
        var allWhitelistedPlayers = ((Vintagestory.Server.PlayerDataManager)_api.PlayerData).WhitelistedPlayers;


        var fullList = from pd in allPlayerData
                       join sp in allServerPlayers on pd.PlayerUID equals sp.PlayerUID into ps
                       from sp in ps.DefaultIfEmpty()
                       join bp in allBannedPlayers on pd.PlayerUID equals bp.PlayerUID into bps
                       from bp in bps.DefaultIfEmpty()
                       join wp in allWhitelistedPlayers on pd.PlayerUID equals wp.PlayerUID into wps
                       from wp in wps.DefaultIfEmpty()
                       select MapToPlayerDTO(pd, sp, bp, wp);

        return fullList.ToList();

    }

    private PlayerDTO MapToPlayerDTO(IServerPlayerData pd, IServerPlayer sp, PlayerEntry bp, PlayerEntry wp)
    {
        var playerDto = new PlayerDTO
        {
            Id = pd.PlayerUID,
            Name = pd.LastKnownPlayername,
            IpAddress = sp?.IpAddress ?? "Offline",
            LanguageCode = sp?.LanguageCode ?? "N/A",
            ConnectionState = sp?.ConnectionState.ToString() ?? "Offline",
            Ping = sp?.Ping ?? 0,
            RolesCode = sp?.Role.Code ?? "N/A",
            FirstJoinDate = pd.FirstJoinDate,
            LastJoinDate = pd.LastJoinDate,
            Privileges = sp?.Privileges.ToArray() ?? new string[0],
            IsAdmin = false,
            IsBanned = bp != null,
            BanReason = bp?.Reason,
            BanBy = bp?.IssuedByPlayerName,
            BanUntil = bp?.UntilDate,
            IsWhitelisted = wp != null,
            WhitelistedReason = wp?.Reason,
            WhitelistedBy = wp?.IssuedByPlayerName,
            WhitelistedUntil = wp?.UntilDate
        };

        return playerDto;
    }

    public PlayerDTO? GetPlayerById(string playerId)
    {
        var player = GetAllPlayers().Where(p => p.Id == playerId).FirstOrDefault();


        if (player == null)
        {
            return null;
        }

        return player;
    }

    public void WhitelistPlayer(string playerId, string reason, string issuedBy)
    {
        var playerData = _api.PlayerData.GetPlayerDataByUid(playerId);
        if (playerData != null)
        {
            //((Vintagestory.Server.PlayerDataManager)_api.PlayerData).WhitelistPlayer()

        }

        return;
    }

    public void KickPlayer(string playerId, string reason)
    {
        var player = _api.Server.Players.Where(p => p.PlayerUID == playerId).FirstOrDefault();
        if (player != null)
        {
            try
            {
                player.Disconnect(reason);
            }
            catch (Exception ex)
            {
                // There is a bug
            }
        }
    }


}
