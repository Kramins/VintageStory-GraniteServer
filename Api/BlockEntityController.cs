using System;
using GenHTTP.Modules.Controllers;
using GenHTTP.Modules.Webservices;

namespace GraniteServer.Api;

/// <summary>
/// Block placement and entity control controller
/// </summary>
public class BlockEntityController
{
    /// <summary>
    /// Set a block at a given location
    /// Linked to: /setblock <block code> <target> command
    /// </summary>
    public object SetBlock(string blockCode, string target)
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
    /// Execute entity commands
    /// Linked to: /executeas <caller> <command without /> command
    /// </summary>
    public object ExecuteAsEntity(string caller, string command)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Control entity via entity selector
    /// Linked to: /entity and /e commands
    /// </summary>
    public object ControlEntity(string selector, string action)
    {
        throw new NotImplementedException();
    }
}
