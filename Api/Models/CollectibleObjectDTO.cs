using Vintagestory.API.Common;

namespace GraniteServer.Api.Models;

public class CollectibleObjectDTO
{
    public int Id { get; internal set; }
    public string Name { get; internal set; }
    public string Type { get; internal set; }
    public int MaxStackSize { get; internal set; }
    public string Class { get; internal set; }
}
