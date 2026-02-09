// using System;
// using System.Threading.Tasks;
// using GraniteServer.Data;
// using GraniteServer.Data.Entities;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Logging;

// namespace Granite.Server.Services.Map;

// /// <summary>
// /// Stored chunk data for rendering.
// /// </summary>
// // public record StoredChunkData(
// //     int ChunkX,
// //     int ChunkZ,
// //     string ContentHash,
// //     int[] RainHeightMap,
// //     int[] SurfaceBlockIds
// // );

// /// <summary>
// /// Implementation of IMapDataStorageService using EF Core.
// /// </summary>
// public class MapDataStorageService : IMapDataStorageService
// {
//     private readonly GraniteDataContext _dbContext;
//     private readonly ILogger<MapDataStorageService> _logger;

//     public MapDataStorageService(
//         GraniteDataContext dbContext,
//         ILogger<MapDataStorageService> logger
//     )
//     {
//         _dbContext = dbContext;
//         _logger = logger;
//     }

//     /// <inheritdoc/>
//     /// <inheritdoc/>
//     public async Task<string?> GetChunkHashAsync(Guid serverId, int chunkX, int chunkZ)
//     {
//         var hash = await _dbContext
//             .MapChunks.Where(c =>
//                 c.ServerId == serverId && c.ChunkX == chunkX && c.ChunkZ == chunkZ
//             )
//             .Select(c => c.ContentHash)
//             .FirstOrDefaultAsync();

//         return hash;
//     }

//     /// <inheritdoc/>
//     public async Task<StoredChunkData?> GetChunkDataAsync(Guid serverId, int chunkX, int chunkZ)
//     {
//         var entity = await _dbContext.MapChunks.FirstOrDefaultAsync(c =>
//             c.ServerId == serverId && c.ChunkX == chunkX && c.ChunkZ == chunkZ
//         );

//         if (entity == null)
//             return null;

//         // Update last accessed time, I don't think we need todo this
//         // entity.LastAccessedAt = DateTime.UtcNow;
//         // await _dbContext.SaveChangesAsync();

//         return new StoredChunkData(
//             entity.ChunkX,
//             entity.ChunkZ,
//             entity.ContentHash,
//             entity.RainHeightMapData,
//             entity.SurfaceBlockIdsData
//         );
//     }

//     /// <inheritdoc/>
//     public async Task<IReadOnlyList<StoredChunkHashDTO>> GetAllChunkHashesAsync(Guid serverId)
//     {
//         var hashes = await _dbContext
//             .MapChunks.Where(c => c.ServerId == serverId)
//             .Select(c => new StoredChunkHashDTO(c.ChunkX, c.ChunkZ, c.ContentHash))
//             .ToListAsync();

//         return hashes;
//     }

//     /// <inheritdoc/>
//     public async Task<int> CleanupOldChunksAsync(Guid serverId, TimeSpan maxAge)
//     {
//         var cutoff = DateTime.UtcNow - maxAge;

//         var oldChunks = await _dbContext
//             .MapChunks.Where(c =>
//                 c.ServerId == serverId && (c.LastAccessedAt == null || c.LastAccessedAt < cutoff)
//             )
//             .ToListAsync();

//         if (oldChunks.Count == 0)
//             return 0;

//         _dbContext.MapChunks.RemoveRange(oldChunks);
//         await _dbContext.SaveChangesAsync();

//         _logger.LogInformation(
//             "Cleaned up {Count} old chunks for server {ServerId}",
//             oldChunks.Count,
//             serverId
//         );

//         return oldChunks.Count;
//     }

//     /// <summary>
//     /// Converts block IDs to byte array for storage.
//     /// </summary>
//     public static byte[] BlockIdsToBytes(int[] blockIds)
//     {
//         var bytes = new byte[blockIds.Length * sizeof(int)];
//         Buffer.BlockCopy(blockIds, 0, bytes, 0, bytes.Length);
//         return bytes;
//     }

//     /// <summary>
//     /// Converts byte array back to block IDs.
//     /// </summary>
//     public static int[] BytesToBlockIds(byte[] data)
//     {
//         var blockIds = new int[data.Length / sizeof(int)];
//         Buffer.BlockCopy(data, 0, blockIds, 0, data.Length);
//         return blockIds;
//     }
// }
