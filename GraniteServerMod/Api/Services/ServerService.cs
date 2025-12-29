using System;
using System.Linq;
using System.Threading.Tasks;
using GraniteServer.Api.Models;
using GraniteServerMod.Data;
using Microsoft.EntityFrameworkCore;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace GraniteServer.Api.Services;

public class ServerService
{
    private readonly ICoreServerAPI _api;
    private readonly ServerCommandService _commandService;
    private readonly GraniteDataContext _dataContext;
    private readonly GraniteServerConfig _config;

    public ServerService(
        ICoreServerAPI api,
        ServerCommandService commandService,
        GraniteServerConfig config,
        GraniteDataContext dataContext
    )
    {
        _api = api;
        _commandService = commandService;
        _config = config;
        _dataContext = dataContext;
    }

    public async Task<ServerConfigDTO> GetServerConfig()
    {
        var config = new ServerConfigDTO
        {
            Port = _api.Server.Config.Port,
            ServerName = _api.Server.Config.ServerName,
            WelcomeMessage = _api.Server.Config.WelcomeMessage,
            MaxClients = _api.Server.Config.MaxClients,
            Password = _api.Server.Config.Password ?? string.Empty,
            MaxChunkRadius = _api.Server.Config.MaxChunkRadius,
            WhitelistMode = _api.Server.Config.WhitelistMode.ToString(),
            AllowPvP = _api.Server.Config.AllowPvP,
            AllowFireSpread = _api.Server.Config.AllowFireSpread,
            AllowFallingBlocks = _api.Server.Config.AllowFallingBlocks,
        };

        return await Task.FromResult(config);
    }

    public async Task<string> UpdateConfigAsync(ServerConfigDTO config)
    {
        var serverEntity = await _dataContext.Servers.FirstOrDefaultAsync(s =>
            s.Id == _config.ServerId
        );

        if (config.ServerName != null)
        {
            _api.Server.Config.ServerName = config.ServerName;
            serverEntity.Name = config.ServerName;
        }

        if (config.WelcomeMessage != null)
        {
            _api.Server.Config.WelcomeMessage = config.WelcomeMessage;
        }

        if (config.MaxClients.HasValue)
        {
            _api.Server.Config.MaxClients = config.MaxClients.Value;
        }

        if (config.Password != null)
        {
            _api.Server.Config.Password = string.IsNullOrEmpty(config.Password)
                ? null
                : config.Password;
        }

        if (config.MaxChunkRadius.HasValue)
        {
            _api.Server.Config.MaxChunkRadius = config.MaxChunkRadius.Value;
        }

        if (
            config.WhitelistMode != null
            && Enum.TryParse(config.WhitelistMode, out EnumWhitelistMode whitelistMode)
        )
        {
            _api.Server.Config.WhitelistMode = whitelistMode;
        }

        if (config.AllowPvP.HasValue)
        {
            _api.Server.Config.AllowPvP = config.AllowPvP.Value;
        }

        if (config.AllowFireSpread.HasValue)
        {
            _api.Server.Config.AllowFireSpread = config.AllowFireSpread.Value;
        }

        if (config.AllowFallingBlocks.HasValue)
        {
            _api.Server.Config.AllowFallingBlocks = config.AllowFallingBlocks.Value;
        }

        // Save the updated configuration
        _api.Server.MarkConfigDirty();
        _dataContext.Servers.Update(serverEntity);
        await _dataContext.SaveChangesAsync();
        await Task.CompletedTask;
        return "Server configuration updated successfully.";
    }

    public async Task<string> AnnounceMessageAsync(string message)
    {
        _api.BroadcastMessageToAllGroups(message, EnumChatType.OthersMessage);
        return await Task.FromResult("Message announced to all players.");
    }
}
