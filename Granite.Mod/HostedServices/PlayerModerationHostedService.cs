using GraniteServer.Messaging.Commands;
using GraniteServer.Messaging.Events;
using GraniteServer.Mod;
using GraniteServer.Services;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace GraniteServer.HostedServices;

/// <summary>
/// Hosted service that handles player moderation commands such as kick, ban, whitelist operations.
/// Subscribes directly to the message bus for player moderation commands.
/// </summary>
public class PlayerModerationHostedService : GraniteHostedServiceBase
{
    private readonly ICoreServerAPI _api;
    private readonly ServerCommandService _commandService;
    private readonly GraniteModConfig _config;

    private PlayerDataManager PlayerDataManager => (PlayerDataManager)_api.PlayerData;

    public PlayerModerationHostedService(
        ICoreServerAPI api,
        ServerCommandService commandService,
        ClientMessageBusService messageBus,
        GraniteModConfig config,
        ILogger logger
    )
        : base(messageBus, logger)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        LogNotification("Starting service...");

        SubscribeToCommand<KickPlayerCommand>(HandleKickPlayerCommand);
        SubscribeToCommand<BanPlayerCommand>(HandleBanPlayerCommand);
        SubscribeToCommand<UnbanPlayerCommand>(HandleUnbanPlayerCommand);
        SubscribeToCommand<WhitelistPlayerCommand>(HandleWhitelistPlayerCommand);
        SubscribeToCommand<UnwhitelistPlayerCommand>(HandleUnwhitelistPlayerCommand);

        LogNotification("Service started");
        return Task.CompletedTask;
    }

    private async Task HandleKickPlayerCommand(KickPlayerCommand command)
    {
        var player = _api
            .Server.Players.Where(p => p.PlayerUID == command.Data!.PlayerId)
            .FirstOrDefault();

        if (player != null)
        {
            try
            {
                LogNotification(
                    "Kicking player {player.PlayerName} (UID: {command.Data!.PlayerId})"
                );

                var result = await _commandService.KickUserAsync(
                    player.PlayerName,
                    command.Data!.Reason
                );

                var kickedEvent = MessageBus.CreateEvent<PlayerKickedEvent>(
                    _config.ServerId,
                    e =>
                    {
                        e.Data!.PlayerUID = command.Data.PlayerId;
                        e.Data!.PlayerName = player.PlayerName;
                        e.Data!.Reason = command.Data.Reason;
                        e.Data!.IssuedBy = "System";
                    }
                );
                MessageBus.Publish(kickedEvent);
            }
            catch (Exception ex)
            {
                LogError("Failed to kick player {player.PlayerName}: {ex.Message}");
            }
        }
        else
        {
            LogWarning("Player with UID {command.Data!.PlayerId} not found for kick command");
        }
    }

    private void HandleBanPlayerCommand(BanPlayerCommand command)
    {
        var playerId = command.Data!.PlayerId;
        var playerName = command.Data.PlayerName;
        var reason = command.Data.Reason;
        var untilDate = command.Data.ExpirationDate;
        var issuedBy = command.Data.IssuedBy;

        LogNotification("Banning player {playerName} (UID: {playerId})");

        var isBanned = PlayerDataManager.BannedPlayers.Any(bp => bp.PlayerUID == playerId);
        if (isBanned)
        {
            PlayerDataManager.BannedPlayers.RemoveAll(bp => bp.PlayerUID == playerId);
        }

        PlayerDataManager.BannedPlayers.Add(
            new PlayerEntry
            {
                PlayerUID = playerId,
                PlayerName = playerName,
                Reason = reason,
                IssuedByPlayerName = issuedBy,
                UntilDate = untilDate ?? DateTime.MaxValue,
            }
        );

        PlayerDataManager.bannedListDirty = true;

        var bannedEvent = MessageBus.CreateEvent<PlayerBannedEvent>(
            _config.ServerId,
            e =>
            {
                e.Data!.PlayerUID = playerId;
                e.Data!.PlayerName = playerName;
                e.Data!.Reason = reason;
                e.Data!.ExpirationDate = untilDate;
                e.Data!.IssuedBy = issuedBy;
            }
        );
        MessageBus.Publish(bannedEvent);
    }

    private void HandleUnbanPlayerCommand(UnbanPlayerCommand command)
    {
        var playerId = command.Data!.PlayerId;

        LogNotification("Unbanning player with UID: {playerId}");

        PlayerDataManager.BannedPlayers.RemoveAll(pe => pe.PlayerUID == playerId);
        PlayerDataManager.bannedListDirty = true;

        var unbannedEvent = MessageBus.CreateEvent<PlayerUnbannedEvent>(
            _config.ServerId,
            e =>
            {
                e.Data!.PlayerUID = playerId;
            }
        );
        MessageBus.Publish(unbannedEvent);
    }

    private void HandleWhitelistPlayerCommand(WhitelistPlayerCommand command)
    {
        var playerId = command.Data!.PlayerId;
        var reason = command.Data.Reason;

        LogNotification("Whitelisting player with UID: {playerId}");

        var isWhitelisted = PlayerDataManager.WhitelistedPlayers.Any(wp =>
            wp.PlayerUID == playerId
        );
        if (isWhitelisted)
        {
            PlayerDataManager.WhitelistedPlayers.RemoveAll(wp => wp.PlayerUID == playerId);
        }

        PlayerDataManager.WhitelistedPlayers.Add(
            new PlayerEntry { PlayerUID = playerId, Reason = reason }
        );

        PlayerDataManager.whiteListDirty = true;

        var whitelistedEvent = MessageBus.CreateEvent<PlayerWhitelistedEvent>(
            _config.ServerId,
            e =>
            {
                e.Data!.PlayerUID = playerId;
            }
        );
        MessageBus.Publish(whitelistedEvent);
    }

    private void HandleUnwhitelistPlayerCommand(UnwhitelistPlayerCommand command)
    {
        var playerId = command.Data!.PlayerId;

        LogNotification("Removing player from whitelist with UID: {playerId}");

        PlayerDataManager.WhitelistedPlayers.RemoveAll(pe => pe.PlayerUID == playerId);
        PlayerDataManager.whiteListDirty = true;

        var unwhitelistedEvent = MessageBus.CreateEvent<PlayerUnwhitelistedEvent>(
            _config.ServerId,
            e =>
            {
                e.Data!.PlayerUID = playerId;
            }
        );
        MessageBus.Publish(unwhitelistedEvent);
    }
}
