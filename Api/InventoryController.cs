using System;
using GenHTTP.Modules.Controllers;
using GenHTTP.Modules.Webservices;

namespace GraniteServer.Api;

/// <summary>
/// Item and inventory management controller
/// </summary>
public class InventoryController
{
    /// <summary>
    /// Give items to a player
    /// Linked to: /giveitem <item code> [quantity] [target] [attributes] command
    /// </summary>
    public object GiveItem(string itemCode, int quantity = 1, string? target = null, string? attributes = null)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Give blocks to a player
    /// Linked to: /giveblock <block code> [quantity] [target] [attributes] command
    /// </summary>
    public object GiveBlock(string blockCode, int quantity = 1, string? target = null, string? attributes = null)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Clear a player's inventory
    /// Linked to: /clearinv command
    /// </summary>
    public object ClearInventory(string? playerName = null)
    {
        throw new NotImplementedException();
    }
}
