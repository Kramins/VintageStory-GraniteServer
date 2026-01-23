using System;
using System.Threading.Tasks;
using Granite.Common.Dto;
using GraniteServer.Messaging.Commands;
using GraniteServer.Messaging.Events;
using GraniteServer.Messaging.Handlers.Commands;
using GraniteServer.Mod;
using GraniteServer.Services;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace GraniteServer.Mod.Handlers.Commands;

public class SyncServerConfigCommandHandler : ICommandHandler<SyncServerConfigCommand>
{
    private readonly ICoreServerAPI _api;
    private readonly ClientMessageBusService _messageBus;
    private readonly GraniteModConfig _config;
    private readonly Vintagestory.API.Common.ILogger _logger;

    public SyncServerConfigCommandHandler(
        ICoreServerAPI api,
        ClientMessageBusService messageBus,
        GraniteModConfig config,
        Vintagestory.API.Common.ILogger logger
    )
    {
        _api = api;
        _messageBus = messageBus;
        _config = config;
        _logger = logger;
    }

    async Task ICommandHandler<SyncServerConfigCommand>.Handle(SyncServerConfigCommand command)
    {
        try
        {
            _logger.Notification("[ServerConfig] Received sync command from control plane.");

            var config = ReadServerConfig();
            var syncEvent = _messageBus.CreateEvent<ServerConfigSyncedEvent>(
                _config.ServerId,
                e =>
                {
                    e.Data.Config = config;
                }
            );

            _messageBus.Publish(syncEvent);
            _logger.Notification("[ServerConfig] Server configuration synced successfully.");
        }
        catch (Exception ex)
        {
            _logger.Error($"[ServerConfig] Failed to sync server configuration: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    private ServerConfigDTO ReadServerConfig()
    {
        var serverConfig = _api.Server.Config;
        return new ServerConfigDTO
        {
            Port = serverConfig.Port,
            ServerName = serverConfig.ServerName,
            WelcomeMessage = serverConfig.WelcomeMessage,
            MaxClients = serverConfig.MaxClients,
            Password = serverConfig.Password ?? string.Empty,
            MaxChunkRadius = serverConfig.MaxChunkRadius,
            WhitelistMode = serverConfig.WhitelistMode.ToString(),
            AllowPvP = serverConfig.AllowPvP,
            AllowFireSpread = serverConfig.AllowFireSpread,
            AllowFallingBlocks = serverConfig.AllowFallingBlocks,
        };
    }
}

public class UpdateServerConfigCommandHandler : ICommandHandler<UpdateServerConfigCommand>
{
    private readonly ICoreServerAPI _api;
    private readonly ClientMessageBusService _messageBus;
    private readonly GraniteModConfig _config;
    private readonly Vintagestory.API.Common.ILogger _logger;

    public UpdateServerConfigCommandHandler(
        ICoreServerAPI api,
        ClientMessageBusService messageBus,
        GraniteModConfig config,
        Vintagestory.API.Common.ILogger logger
    )
    {
        _api = api;
        _messageBus = messageBus;
        _config = config;
        _logger = logger;
    }

    async Task ICommandHandler<UpdateServerConfigCommand>.Handle(
        UpdateServerConfigCommand command
    )
    {
        try
        {
            _logger.Notification(
                "[ServerConfig] Received update command from control plane. Applying changes..."
            );

            var configDto = command.Data.Config;
            var serverConfig = _api.Server.Config;

            // Update config properties (only non-null values)
            if (configDto.ServerName != null)
            {
                _logger.Notification(
                    $"[ServerConfig] ServerName: {serverConfig.ServerName} -> {configDto.ServerName}"
                );
                serverConfig.ServerName = configDto.ServerName;
            }

            if (configDto.WelcomeMessage != null)
            {
                _logger.Notification("[ServerConfig] WelcomeMessage updated.");
                serverConfig.WelcomeMessage = configDto.WelcomeMessage;
            }

            if (configDto.MaxClients.HasValue)
            {
                _logger.Notification(
                    $"[ServerConfig] MaxClients: {serverConfig.MaxClients} -> {configDto.MaxClients.Value}"
                );
                serverConfig.MaxClients = configDto.MaxClients.Value;
            }

            if (configDto.Password != null)
            {
                _logger.Notification("[ServerConfig] Password updated.");
                serverConfig.Password = string.IsNullOrEmpty(configDto.Password)
                    ? null
                    : configDto.Password;
            }

            if (configDto.MaxChunkRadius.HasValue)
            {
                _logger.Notification(
                    $"[ServerConfig] MaxChunkRadius: {serverConfig.MaxChunkRadius} -> {configDto.MaxChunkRadius.Value}"
                );
                serverConfig.MaxChunkRadius = configDto.MaxChunkRadius.Value;
            }

            if (
                configDto.WhitelistMode != null
                && Enum.TryParse(configDto.WhitelistMode, out EnumWhitelistMode whitelistMode)
            )
            {
                _logger.Notification(
                    $"[ServerConfig] WhitelistMode: {serverConfig.WhitelistMode} -> {whitelistMode}"
                );
                serverConfig.WhitelistMode = whitelistMode;
            }

            if (configDto.AllowPvP.HasValue)
            {
                _logger.Notification(
                    $"[ServerConfig] AllowPvP: {serverConfig.AllowPvP} -> {configDto.AllowPvP.Value}"
                );
                serverConfig.AllowPvP = configDto.AllowPvP.Value;
            }

            if (configDto.AllowFireSpread.HasValue)
            {
                _logger.Notification(
                    $"[ServerConfig] AllowFireSpread: {serverConfig.AllowFireSpread} -> {configDto.AllowFireSpread.Value}"
                );
                serverConfig.AllowFireSpread = configDto.AllowFireSpread.Value;
            }

            if (configDto.AllowFallingBlocks.HasValue)
            {
                _logger.Notification(
                    $"[ServerConfig] AllowFallingBlocks: {serverConfig.AllowFallingBlocks} -> {configDto.AllowFallingBlocks.Value}"
                );
                serverConfig.AllowFallingBlocks = configDto.AllowFallingBlocks.Value;
            }

            // Mark config as dirty to persist changes
            _api.Server.MarkConfigDirty();
            _logger.Notification("[ServerConfig] Configuration marked as dirty for persistence.");

            // Publish sync event to confirm changes
            var syncEvent = _messageBus.CreateEvent<ServerConfigSyncedEvent>(
                _config.ServerId,
                e =>
                {
                    e.Data.Config = ReadServerConfig();
                }
            );
            _messageBus.Publish(syncEvent);

            _logger.Notification("[ServerConfig] Server configuration updated successfully.");
        }
        catch (Exception ex)
        {
            _logger.Error($"[ServerConfig] Failed to update server configuration: {ex.Message}");
        }

        await Task.CompletedTask;
    }

    private ServerConfigDTO ReadServerConfig()
    {
        var serverConfig = _api.Server.Config;
        return new ServerConfigDTO
        {
            Port = serverConfig.Port,
            ServerName = serverConfig.ServerName,
            WelcomeMessage = serverConfig.WelcomeMessage,
            MaxClients = serverConfig.MaxClients,
            Password = serverConfig.Password ?? string.Empty,
            MaxChunkRadius = serverConfig.MaxChunkRadius,
            WhitelistMode = serverConfig.WhitelistMode.ToString(),
            AllowPvP = serverConfig.AllowPvP,
            AllowFireSpread = serverConfig.AllowFireSpread,
            AllowFallingBlocks = serverConfig.AllowFallingBlocks,
        };
    }
}
