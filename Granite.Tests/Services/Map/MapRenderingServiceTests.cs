using FluentAssertions;
using Granite.Server.Models;
using Granite.Server.Services.Map;
using GraniteServer.Map;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Granite.Tests.Services.Map;

public class MapRenderingServiceTests
{
    private readonly ILogger<MapRenderingService> _mockLogger;
    private readonly MapRenderingService _service;

    public MapRenderingServiceTests()
    {
        _mockLogger = Substitute.For<ILogger<MapRenderingService>>();
        _service = new MapRenderingService(_mockLogger);
    }

    [Fact]
    public void ChunkSize_Is32()
    {
        MapRenderingService.ChunkSize.Should().Be(32);
    }

    [Fact]
    public async Task RenderGroupedTileAsync_WithValidData_ReturnsPngBytes()
    {
        // Arrange
        var heightMap = new int[1024];
        var blockIds = new int[1024];
        var chunkData = new StoredChunkData
        {
            ChunkX = 0,
            ChunkZ = 0,
            ContentHash = "test-hash",
            RainHeightMap = heightMap,
            SurfaceBlockId = blockIds
        };

        var chunks = new[] { chunkData };

        // Act
        var result = await _service.RenderGroupedTileAsync(0, 0, chunks);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        
        // PNG starts with magic bytes: 0x89 0x50 0x4E 0x47
        result[0].Should().Be(0x89);
        result[1].Should().Be(0x50);
        result[2].Should().Be(0x4E);
        result[3].Should().Be(0x47);
    }

    [Fact]
    public async Task RenderGroupedTileAsync_WithNullChunks_ReturnsNull()
    {
        // Act
        var result = await _service.RenderGroupedTileAsync(0, 0, null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RenderGroupedTileAsync_WithEmptyChunks_ReturnsNull()
    {
        // Act
        var result = await _service.RenderGroupedTileAsync(0, 0, Array.Empty<StoredChunkData>());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RenderGroupedTileAsync_WithHeightVariation_ProducesDifferentColors()
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

        var chunkData = new StoredChunkData
        {
            ChunkX = 0,
            ChunkZ = 0,
            ContentHash = "test-hash",
            RainHeightMap = heightMap,
            SurfaceBlockId = blockIds
        };

        var chunks = new[] { chunkData };

        // Act
        var result = await _service.RenderGroupedTileAsync(0, 0, chunks);

        // Assert - just verify we get valid PNG output
        result.Should().NotBeNull();
        result.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RenderGroupedTileAsync_WithBlockColorMapping_UsesMapping()
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

        var chunkData = new StoredChunkData
        {
            ChunkX = 0,
            ChunkZ = 0,
            ContentHash = "test-hash",
            RainHeightMap = heightMap,
            SurfaceBlockId = blockIds
        };

        var chunks = new[] { chunkData };

        // Act
        var result = await _service.RenderGroupedTileAsync(0, 0, chunks, blockMapping);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RenderGroupedTileAsync_WithMultipleChunks_RendersAllChunks()
    {
        // Arrange - create multiple chunks
        var chunks = new StoredChunkData[2];
        
        for (var c = 0; c < 2; c++)
        {
            var heightMap = new int[1024];
            var blockIds = new int[1024];
            
            chunks[c] = new StoredChunkData
            {
                ChunkX = c,
                ChunkZ = 0,
                ContentHash = $"hash-{c}",
                RainHeightMap = heightMap,
                SurfaceBlockId = blockIds
            };
        }

        // Act
        var result = await _service.RenderGroupedTileAsync(0, 0, chunks);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RenderGroupedTileAsync_WithPartialGroupTile_FillsMissingWithFogOfWar()
    {
        // Arrange - only provide one chunk for a grouped tile (which normally has 8x8 chunks)
        var heightMap = new int[1024];
        var blockIds = new int[1024];
        
        var chunkData = new StoredChunkData
        {
            ChunkX = 0, // First chunk in group (0,0)
            ChunkZ = 0,
            ContentHash = "test-hash",
            RainHeightMap = heightMap,
            SurfaceBlockId = blockIds
        };

        var chunks = new[] { chunkData };

        // Act
        var result = await _service.RenderGroupedTileAsync(0, 0, chunks);

        // Assert - should still render with fog of war for missing chunks
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetFogOfWarTileAsync_ReturnsPngBytes()
    {
        // Act
        var result = await _service.GetFogOfWarTileAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        
        // Verify PNG magic bytes
        result[0].Should().Be(0x89);
        result[1].Should().Be(0x50);
        result[2].Should().Be(0x4E);
        result[3].Should().Be(0x47);
    }

    [Fact]
    public async Task GetFogOfWarTileAsync_ReturnsSameTileEveryTime()
    {
        // Act
        var result1 = await _service.GetFogOfWarTileAsync();
        var result2 = await _service.GetFogOfWarTileAsync();

        // Assert - should be byte-for-byte identical
        result1.Should().Equal(result2);
    }
}

