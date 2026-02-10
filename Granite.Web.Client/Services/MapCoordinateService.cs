namespace Granite.Web.Client.Services;

/// <summary>
/// Service for transforming coordinates between Vintage Story block space and OpenLayers map space.
/// </summary>
public class MapCoordinateService
{
    /// <summary>
    /// Convert Vintage Story block coordinates to OpenLayers map coordinates.
    /// OpenLayers uses [x, -z] with origin at top-left, VS uses (x, z) with z increasing northward.
    /// </summary>
    public (double MapX, double MapY) BlockToMapCoords(float blockX, float blockZ)
    {
        return (blockX, -blockZ);
    }

    /// <summary>
    /// Check if a block position is within the given map bounds.
    /// </summary>
    public bool IsInBounds(
        float blockX,
        float blockZ,
        int minChunkX,
        int maxChunkX,
        int minChunkZ,
        int maxChunkZ
    )
    {
        var chunkX = (int)Math.Floor(blockX / 32);
        var chunkZ = (int)Math.Floor(blockZ / 32);

        return chunkX >= minChunkX
            && chunkX <= maxChunkX
            && chunkZ >= minChunkZ
            && chunkZ <= maxChunkZ;
    }

    /// <summary>
    /// Calculate straight-line distance between two block positions (ignoring Y/height).
    /// </summary>
    public double Distance(float x1, float z1, float x2, float z2)
    {
        return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(z2 - z1, 2));
    }

    /// <summary>
    /// Get the tile coordinates that contain this block position.
    /// Each tile is 256x256 blocks (8x8 chunks).
    /// </summary>
    public (int TileX, int TileZ) GetContainingTile(float blockX, float blockZ)
    {
        return ((int)Math.Floor(blockX / 256), (int)Math.Floor(blockZ / 256));
    }

    /// <summary>
    /// Get the chunk coordinates that contain this block position.
    /// Each chunk is 32x32 blocks.
    /// </summary>
    public (int ChunkX, int ChunkZ) GetContainingChunk(float blockX, float blockZ)
    {
        return ((int)Math.Floor(blockX / 32), (int)Math.Floor(blockZ / 32));
    }

    /// <summary>
    /// Convert map coordinates back to block coordinates (reverse of BlockToMapCoords).
    /// </summary>
    public (double BlockX, double BlockZ) MapToBlockCoords(double mapX, double mapY)
    {
        return (mapX, -mapY);
    }
}
