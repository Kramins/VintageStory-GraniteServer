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
                "[ServerConfig] Received update command from control plane. Comparing and applying changes..."
            );

            var configDto = command.Data.Config;
            var serverConfig = _api.Server.Config;
            var hasChanges = false;

            // Compare and update config properties (only apply changed values)
            if (configDto.ServerName != null && serverConfig.ServerName != configDto.ServerName)
            {
                _logger.Notification(
                    $"[ServerConfig] ServerName: {serverConfig.ServerName} -> {configDto.ServerName}"
                );
                serverConfig.ServerName = configDto.ServerName;
                hasChanges = true;
            }

            if (configDto.WelcomeMessage != null && serverConfig.WelcomeMessage != configDto.WelcomeMessage)
            {
                _logger.Notification($"[ServerConfig] WelcomeMessage: {serverConfig.WelcomeMessage} -> {configDto.WelcomeMessage}");
                serverConfig.WelcomeMessage = configDto.WelcomeMessage;
                hasChanges = true;
            }

            if (configDto.MaxClients.HasValue && serverConfig.MaxClients != configDto.MaxClients.Value)
            {
                _logger.Notification(
                    $"[ServerConfig] MaxClients: {serverConfig.MaxClients} -> {configDto.MaxClients.Value}"
                );
                serverConfig.MaxClients = configDto.MaxClients.Value;
                hasChanges = true;
            }

            var newPassword = string.IsNullOrEmpty(configDto.Password) ? null : configDto.Password;
            if (serverConfig.Password != newPassword)
            {
                _logger.Notification("[ServerConfig] Password updated.");
                serverConfig.Password = newPassword;
                hasChanges = true;
            }

            if (configDto.MaxChunkRadius.HasValue && serverConfig.MaxChunkRadius != configDto.MaxChunkRadius.Value)
            {
                _logger.Notification(
                    $"[ServerConfig] MaxChunkRadius: {serverConfig.MaxChunkRadius} -> {configDto.MaxChunkRadius.Value}"
                );
                serverConfig.MaxChunkRadius = configDto.MaxChunkRadius.Value;
                hasChanges = true;
            }

            if (
                configDto.WhitelistMode != null
                && Enum.TryParse(configDto.WhitelistMode, out EnumWhitelistMode whitelistMode)
                && serverConfig.WhitelistMode != whitelistMode
            )
            {
                _logger.Notification(
                    $"[ServerConfig] WhitelistMode: {serverConfig.WhitelistMode} -> {whitelistMode}"
                );
                serverConfig.WhitelistMode = whitelistMode;
                hasChanges = true;
            }

            if (configDto.AllowPvP.HasValue && serverConfig.AllowPvP != configDto.AllowPvP.Value)
            {
                _logger.Notification(
                    $"[ServerConfig] AllowPvP: {serverConfig.AllowPvP} -> {configDto.AllowPvP.Value}"
                );
                serverConfig.AllowPvP = configDto.AllowPvP.Value;
                hasChanges = true;
            }

            if (configDto.AllowFireSpread.HasValue && serverConfig.AllowFireSpread != configDto.AllowFireSpread.Value)
            {
                _logger.Notification(
                    $"[ServerConfig] AllowFireSpread: {serverConfig.AllowFireSpread} -> {configDto.AllowFireSpread.Value}"
                );
                serverConfig.AllowFireSpread = configDto.AllowFireSpread.Value;
                hasChanges = true;
            }

            if (configDto.AllowFallingBlocks.HasValue && serverConfig.AllowFallingBlocks != configDto.AllowFallingBlocks.Value)
            {
                _logger.Notification(
                    $"[ServerConfig] AllowFallingBlocks: {serverConfig.AllowFallingBlocks} -> {configDto.AllowFallingBlocks.Value}"
                );
                serverConfig.AllowFallingBlocks = configDto.AllowFallingBlocks.Value;
                hasChanges = true;
            }

            // Only mark config as dirty and persist if changes were made
            if (hasChanges)
            {
                _api.Server.MarkConfigDirty();
                _logger.Notification("[ServerConfig] Configuration marked as dirty for persistence.");
            }
            else
            {
                _logger.Notification("[ServerConfig] No changes detected, config already matches.");
            }

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
