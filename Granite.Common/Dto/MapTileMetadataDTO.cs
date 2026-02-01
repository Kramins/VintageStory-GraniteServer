using System;

namespace Granite.Common.Dto;

public record MapTileMetadataDTO
{
    public int ChunkX { get; init; }
    public int ChunkZ { get; init; }
    public string ChunkHash { get; init; } = string.Empty;
    public int Width { get; init; }
    public int Height { get; init; }
    public DateTime ExtractedAt { get; init; }
}
