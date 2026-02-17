using Granite.Common.Dto;
using GraniteServer.Messaging.Commands;
using GraniteServer.Messaging.Events;
using GraniteServer.Mod;
using GraniteServer.Services;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace GraniteServer.HostedServices;

/// <summary>
/// Hosted service that handles server-level management commands including configuration and communication.
/// Subscribes directly to the message bus for server management commands.
/// </summary>
public class ServerManagementHostedService : GraniteHostedServiceBase
{
    private readonly ICoreServerAPI _api;
    private readonly ServerCommandService _commandService;
    private readonly GraniteModConfig _config;

    public ServerManagementHostedService(
        ICoreServerAPI api,
        ServerCommandService commandService,
        ClientMessageBusService messageBus,
        GraniteModConfig config,
        ILogger logger
    ) : base(messageBus, logger)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        LogNotification("Starting service...");

        SubscribeToCommand<SyncServerConfigCommand>(HandleSyncServerConfigCommand);
        SubscribeToCommand<UpdateServerConfigCommand>(HandleUpdateServerConfigCommand);
        SubscribeToCommand<AnnounceMessageCommand>(HandleAnnounceMessageCommand);

        LogNotification("Service started");
        return Task.CompletedTask;
    }

    private void HandleSyncServerConfigCommand(SyncServerConfigCommand command)
    {
        LogNotification("Received sync command from control plane");

        var config = ReadServerConfig();
        var syncEvent = MessageBus.CreateEvent<ServerConfigSyncedEvent>(
            _config.ServerId,
            e =>
            {
                e.Data.Config = config;
            }
        );

        MessageBus.Publish(syncEvent);
        LogNotification("Server configuration synced successfully");
    }

    private void HandleUpdateServerConfigCommand(UpdateServerConfigCommand command)
    {
        LogNotification("Received update command from control plane. Comparing and applying changes...");

        var configDto = command.Data.Config;
        var serverConfig = _api.Server.Config;
        var hasChanges = false;

        // Compare and update config properties (only apply changed values)
        if (configDto.ServerName != null && serverConfig.ServerName != configDto.ServerName)
        {
            LogNotification("ServerName: {serverConfig.ServerName} -> {configDto.ServerName}");
            serverConfig.ServerName = configDto.ServerName;
            hasChanges = true;
        }

        if (configDto.WelcomeMessage != null && serverConfig.WelcomeMessage != configDto.WelcomeMessage)
        {
            LogNotification("WelcomeMessage: {serverConfig.WelcomeMessage} -> {configDto.WelcomeMessage}");
            serverConfig.WelcomeMessage = configDto.WelcomeMessage;
            hasChanges = true;
        }

        if (configDto.MaxClients.HasValue && serverConfig.MaxClients != configDto.MaxClients.Value)
        {
            LogNotification("MaxClients: {serverConfig.MaxClients} -> {configDto.MaxClients.Value}");
            serverConfig.MaxClients = configDto.MaxClients.Value;
            hasChanges = true;
        }

        var newPassword = string.IsNullOrEmpty(configDto.Password) ? null : configDto.Password;
        if (serverConfig.Password != newPassword)
        {
            LogNotification("Password updated");
            serverConfig.Password = newPassword;
            hasChanges = true;
        }

        if (configDto.MaxChunkRadius.HasValue && serverConfig.MaxChunkRadius != configDto.MaxChunkRadius.Value)
        {
            LogNotification("MaxChunkRadius: {serverConfig.MaxChunkRadius} -> {configDto.MaxChunkRadius.Value}");
            serverConfig.MaxChunkRadius = configDto.MaxChunkRadius.Value;
            hasChanges = true;
        }

        if (configDto.WhitelistMode.HasValue)
        {
            var whitelistMode = configDto.WhitelistMode.Value ? EnumWhitelistMode.On : EnumWhitelistMode.Off;
            if (serverConfig.WhitelistMode != whitelistMode)
            {
                LogNotification("WhitelistMode: {serverConfig.WhitelistMode} -> {whitelistMode}");
                serverConfig.WhitelistMode = whitelistMode;
                hasChanges = true;
            }
        }

        if (configDto.AllowPvP.HasValue && serverConfig.AllowPvP != configDto.AllowPvP.Value)
        {
            LogNotification("AllowPvP: {serverConfig.AllowPvP} -> {configDto.AllowPvP.Value}");
            serverConfig.AllowPvP = configDto.AllowPvP.Value;
            hasChanges = true;
        }

        if (configDto.AllowFireSpread.HasValue && serverConfig.AllowFireSpread != configDto.AllowFireSpread.Value)
        {
            LogNotification("AllowFireSpread: {serverConfig.AllowFireSpread} -> {configDto.AllowFireSpread.Value}");
            serverConfig.AllowFireSpread = configDto.AllowFireSpread.Value;
            hasChanges = true;
        }

        if (configDto.AllowFallingBlocks.HasValue && serverConfig.AllowFallingBlocks != configDto.AllowFallingBlocks.Value)
        {
            LogNotification("AllowFallingBlocks: {serverConfig.AllowFallingBlocks} -> {configDto.AllowFallingBlocks.Value}");
            serverConfig.AllowFallingBlocks = configDto.AllowFallingBlocks.Value;
            hasChanges = true;
        }

        // Only mark config as dirty and persist if changes were made
        if (hasChanges)
        {
            _api.Server.MarkConfigDirty();
            LogNotification("Configuration marked as dirty for persistence");
        }
        else
        {
            LogNotification("No changes detected, config already matches");
        }

        LogNotification("Server configuration updated successfully");
    }

    private async Task HandleAnnounceMessageCommand(AnnounceMessageCommand command)
    {
        var message = command.Data?.Message;
        if (string.IsNullOrWhiteSpace(message))
        {
            LogWarning("Received AnnounceMessageCommand with empty message");
            return;
        }

        LogNotification("Broadcasting message: {message}");
        var result = await _commandService.AnnounceMessageAsync(message);
        LogNotification("Announcement result: {result}");
    }

    private ServerConfigDTO ReadServerConfig()
    {
        var serverConfig = _api.Server.Config;
        
        // Convert WhitelistMode enum to boolean
        bool whitelistMode = serverConfig.WhitelistMode == EnumWhitelistMode.On;
        
        return new ServerConfigDTO
        {
            Port = serverConfig.Port,
            ServerName = serverConfig.ServerName,
            WelcomeMessage = serverConfig.WelcomeMessage,
            MaxClients = serverConfig.MaxClients,
            Password = serverConfig.Password ?? string.Empty,
            MaxChunkRadius = serverConfig.MaxChunkRadius,
            WhitelistMode = whitelistMode,
            AllowPvP = serverConfig.AllowPvP,
            AllowFireSpread = serverConfig.AllowFireSpread,
            AllowFallingBlocks = serverConfig.AllowFallingBlocks,
        };
    }
}
