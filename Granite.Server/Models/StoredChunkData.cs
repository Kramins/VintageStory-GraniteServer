using System;

namespace Granite.Server.Models;

public class StoredChunkData
{
    public int ChunkX { get; set; }
    public int ChunkZ { get; set; }
    public string? ContentHash { get; set; }
    public int[]? RainHeightMap { get; set; }
    public int[]? SurfaceBlockId { get; set; }
}
