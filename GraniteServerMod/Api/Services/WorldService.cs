using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GenHTTP.Engine.Internal;
using GraniteServer.Api.Models;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace GraniteServer.Api.Services;

public class WorldService
{
    private ICoreServerAPI _api;
    private ServerCommandService _serverCommandService;

    public WorldService(ICoreServerAPI api, ServerCommandService serverCommandService)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _serverCommandService =
            serverCommandService ?? throw new ArgumentNullException(nameof(serverCommandService));
    }

    public async Task<List<CollectibleObjectDTO>> GetAllCollectiblesAsync()
    {
        var allCollectibles = _api.World.Collectibles.Select(c => MapCollectibleToDTO(c)).ToList();

        return await Task.FromResult(allCollectibles);
    }

    public async Task<string> SaveWorldAsync()
    {
        return await _serverCommandService.AutoSaveWorldAsync();
    }

    private CollectibleObjectDTO MapCollectibleToDTO(CollectibleObject collectible)
    {
        var dto = new CollectibleObjectDTO { Id = collectible.Id };
        var text = collectible.ItemClass.Name();
        var itemName = Lang.GetMatching(
            collectible.Code?.Domain + ":" + text + "-" + collectible.Code?.Path,
            [""]
        );
        dto.Name = itemName;
        dto.Class = collectible.Class;
        dto.MaxStackSize = collectible.MaxStackSize;
        if (collectible is Item item)
        {
            dto.Type = "item";
        }
        else if (collectible is Block block)
        {
            dto.Type = "block";
        }
        return dto;
    }
}
