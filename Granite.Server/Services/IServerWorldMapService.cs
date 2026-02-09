using Granite.Common.Dto;

namespace Granite.Server.Services;

public interface IServerWorldMapService
{
    Task<WorldMapBoundsDTO?> GetWorldBoundsAsync(Guid serverId);
    Task<byte[]?> GetTileImageAsync(Guid serverId, int chunkX, int chunkZ);
    Task<byte[]?> GetGroupedTileImageAsync(Guid serverId, int groupX, int groupZ);
    Task<MapTileMetadataDTO?> GetTileMetadataAsync(Guid serverId, int chunkX, int chunkZ);
    Task<byte[]?> GetNotFoundTileImageAsync(Guid serverid, int chunkX, int chunkZ);
    Task<List<StoredChunkHashDTO>> GetAllChunkHashesAsync(Guid serverId);
    Task<bool> StoreChunkDataAsync(
        Guid serverId,
        int chunkX,
        int chunkZ,
        string contentHash,
        int[] rainHeightMap,
        int[] surfaceBlockIds,
        DateTime extractedAt
    );
}
