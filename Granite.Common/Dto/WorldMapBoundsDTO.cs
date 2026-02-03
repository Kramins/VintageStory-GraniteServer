namespace Granite.Common.Dto;

public record WorldMapBoundsDTO
{
    public int MinChunkX { get; init; }
    public int MaxChunkX { get; init; }
    public int MinChunkZ { get; init; }
    public int MaxChunkZ { get; init; }
    public int TotalChunks { get; init; }
}
