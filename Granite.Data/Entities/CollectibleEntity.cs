using System;

namespace GraniteServer.Data.Entities;

public class CollectibleEntity
{
    public Guid Id { get; set; }
    public Guid ServerId { get; set; }
    public int CollectibleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int MaxStackSize { get; set; }
    public string? Class { get; set; }
    public DateTime LastSynced { get; set; }

    // Navigation properties
    public ServerEntity? Server { get; set; }
}
