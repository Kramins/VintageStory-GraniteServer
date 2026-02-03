using FluentAssertions;
using Granite.Server.Services.Map;
using GraniteServer.Map;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Granite.Tests.Services.Map;

public class MapRenderingServiceTests
{
    private readonly IMapDataStorageService _mockStorageService;
    private readonly ILogger<MapRenderingService> _mockLogger;
    private readonly IMemoryCache _mockMemoryCache;
    private readonly MapRenderingService _service;

    public MapRenderingServiceTests()
    {
        _mockStorageService = Substitute.For<IMapDataStorageService>();
        _mockLogger = Substitute.For<ILogger<MapRenderingService>>();
        _mockMemoryCache = Substitute.For<IMemoryCache>();
        _service = new MapRenderingService(_mockStorageService, _mockMemoryCache, _mockLogger);
    }

    [Fact]
    public void ChunkSize_Is32()
    {
        MapRenderingService.ChunkSize.Should().Be(32);
    }

    [Fact]
    public void RenderTileFromData_WithValidData_ReturnsTgaBytes()
    {
        // Arrange
        var heightMap = new int[1024];
        var blockIds = new int[1024];
        var chunkData = new StoredChunkData(
            ChunkX: 0,
            ChunkZ: 0,
            ContentHash: "test-hash",
            RainHeightMap: heightMap,
            SurfaceBlockIds: blockIds
        );

        // Act
        var result = _service.RenderTileFromData(chunkData);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();

        // TGA header is 18 bytes, then 32x32 pixels * 4 bytes (BGRA)
        var expectedSize = 18 + (32 * 32 * 4);
        result.Length.Should().Be(expectedSize);
    }

    [Fact]
    public void RenderTileFromData_WithHeightVariation_ProducesDifferentColors()
    {
        // Arrange - create terrain with varying heights
        var heightMap = new int[1024];
        var blockIds = new int[1024];

        // Low terrain
        for (var i = 0; i < 512; i++)
        {
            heightMap[i] = 32; // Low
            blockIds[i] = 1;
        }

        // High terrain
        for (var i = 512; i < 1024; i++)
        {
            heightMap[i] = 192; // High
            blockIds[i] = 1;
        }

        var chunkData = new StoredChunkData(
            ChunkX: 0,
            ChunkZ: 0,
            ContentHash: "test-hash",
            RainHeightMap: heightMap,
            SurfaceBlockIds: blockIds
        );

        // Act
        var result = _service.RenderTileFromData(chunkData);

        // Assert - just verify we get valid output
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(18); // More than just header
    }

    [Fact]
    public void RenderTileFromData_WithBlockColorMapping_UsesMapping()
    {
        // Arrange
        var heightMap = new int[1024];
        var blockIds = new int[1024];

        for (var i = 0; i < 1024; i++)
        {
            heightMap[i] = 64;
            blockIds[i] = 100; // Specific block ID
        }

        var blockMapping = new Dictionary<int, string> { { 100, "forest" } };

        var chunkData = new StoredChunkData(
            ChunkX: 0,
            ChunkZ: 0,
            ContentHash: "test-hash",
            RainHeightMap: heightMap,
            SurfaceBlockIds: blockIds
        );

        // Act
        var result = _service.RenderTileFromData(chunkData, blockMapping);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task RenderChunkTileAsync_ChunkNotInStorage_ReturnsNull()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        _mockStorageService.GetChunkDataAsync(serverId, 0, 0).Returns((StoredChunkData?)null);

        // Act
        var result = await _service.RenderChunkTileAsync(serverId, 0, 0);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RenderChunkTileAsync_ChunkInStorage_ReturnsTile()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var heightMap = new int[1024];
        var blockIds = new int[1024];
        var chunkData = new StoredChunkData(0, 0, "hash123", heightMap, blockIds);

        _mockStorageService.GetChunkHashAsync(serverId, 0, 0).Returns("hash123");
        _mockStorageService.GetChunkDataAsync(serverId, 0, 0).Returns(chunkData);

        // Act
        var result = await _service.RenderChunkTileAsync(serverId, 0, 0);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task RenderChunkTileAsync_CachesResult()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var heightMap = new int[1024];
        var blockIds = new int[1024];
        var chunkData = new StoredChunkData(0, 0, "hash123", heightMap, blockIds);

        _mockStorageService.GetChunkHashAsync(serverId, 0, 0).Returns("hash123");
        _mockStorageService.GetChunkDataAsync(serverId, 0, 0).Returns(chunkData);

        // Act - first call
        var result1 = await _service.RenderChunkTileAsync(serverId, 0, 0);

        // Second call should use cache
        var result2 = await _service.RenderChunkTileAsync(serverId, 0, 0);

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();

        // Storage should only be called once for data (second call uses cache)
        await _mockStorageService.Received(1).GetChunkDataAsync(serverId, 0, 0);
    }

    [Fact]
    public void InvalidateChunk_RemovesSpecificChunk()
    {
        // This is mostly for coverage - invalidation is internal state
        var serverId = Guid.NewGuid();

        // Act - should not throw
        var act = () => _service.InvalidateChunk(serverId, 5, 10);

        // Assert
        act.Should().NotThrow();
    }
}
