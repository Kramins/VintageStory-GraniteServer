using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraniteServer.Api.Models;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace GraniteServer.Api.Services;

public class WorldService
{
    private ICoreServerAPI _api;

    public WorldService(ICoreServerAPI api)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
    }

    public async Task<List<CollectibleObjectDTO>> GetAllCollectiblesAsync()
    {
        var allCollectibles = _api.World.Collectibles.Select(c => MapCollectibleToDTO(c)).ToList();

        return await Task.FromResult(allCollectibles);
    }

    private CollectibleObjectDTO MapCollectibleToDTO(CollectibleObject collectible)
    {
        var dto = new CollectibleObjectDTO { Id = collectible.Id };

        if (collectible is Item item)
        {
            dto.Name = item.Code.GetName();
            dto.Type = "item";
        }
        else if (collectible is Block block)
        {
            dto.Name = block.Code.GetName();
            dto.Type = "block";
        }
        return dto;
    }
}
