using FluentAssertions;
using Granite.Mod.Services.Map;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Granite.Mod.Tests.Services.Map;

/// <summary>
/// Unit tests for MapDataExtractionService.
/// Note: Full integration tests require a running Vintage Story server.
/// These tests focus on the static helper methods and simple behaviors.
/// </summary>
public class MapDataExtractionServiceTests
{
    [Fact]
    public void ChunkSizeConst_Is32()
    {
        // The chunk size should be 32
        MapDataExtractionService.ChunkSizeConst.Should().Be(32);
    }

    [Fact]
    public void MapPositionInfo_Record_StoresAllProperties()
    {
        // Arrange & Act
        var info = new MapPositionInfo(
            WorldX: 100,
            WorldZ: 200,
            Height: 64,
            BlockId: 1,
            BlockCode: "game:stone",
            ColorCode: "land"
        );

        // Assert
        info.WorldX.Should().Be(100);
        info.WorldZ.Should().Be(200);
        info.Height.Should().Be(64);
        info.BlockId.Should().Be(1);
        info.BlockCode.Should().Be("game:stone");
        info.ColorCode.Should().Be("land");
    }

    [Fact]
    public void MapPositionInfo_Equality_WorksCorrectly()
    {
        // Arrange
        var info1 = new MapPositionInfo(100, 200, 64, 1, "stone", "land");
        var info2 = new MapPositionInfo(100, 200, 64, 1, "stone", "land");
        var info3 = new MapPositionInfo(100, 200, 64, 2, "stone", "land");

        // Assert
        info1.Should().Be(info2);
        info1.Should().NotBe(info3);
    }

    [Fact]
    public void MapChunkExtractedData_Record_StoresAllProperties()
    {
        // Arrange
        var heightMap = new ushort[1024];
        var blockIds = new int[1024];
        var extractedAt = DateTime.UtcNow;

        // Act
        var data = new MapChunkExtractedData(
            ChunkX: 5,
            ChunkZ: 10,
            ContentHash: "abc123",
            RainHeightMap: heightMap,
            SurfaceBlockIds: blockIds,
            ExtractedAt: extractedAt
        );

        // Assert
        data.ChunkX.Should().Be(5);
        data.ChunkZ.Should().Be(10);
        data.ContentHash.Should().Be("abc123");
        data.RainHeightMap.Should().BeSameAs(heightMap);
        data.SurfaceBlockIds.Should().BeSameAs(blockIds);
        data.ExtractedAt.Should().Be(extractedAt);
    }

    [Fact]
    public void ChunkHashData_Record_StoresAllProperties()
    {
        // Act
        var data = new ChunkHashData(ChunkX: 5, ChunkZ: 10, ContentHash: "abc123");

        // Assert
        data.ChunkX.Should().Be(5);
        data.ChunkZ.Should().Be(10);
        data.ContentHash.Should().Be("abc123");
    }

    [Fact]
    public void Constructor_ThrowsOnNullApi()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();

        // Act & Assert
        var act = () => new MapDataExtractionService(null!, logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("api");
    }

    [Fact]
    public void Constructor_ThrowsOnNullLogger()
    {
        // Arrange
        var api = Substitute.For<ICoreServerAPI>();

        // Act & Assert
        var act = () => new MapDataExtractionService(api, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void MapSizeX_WhenWorldNull_ReturnsZero()
    {
        // Arrange
        var api = Substitute.For<ICoreServerAPI>();
        var logger = Substitute.For<ILogger>();
        api.World.Returns((IServerWorldAccessor?)null);

        var service = new MapDataExtractionService(api, logger);

        // Act
        var result = service.MapSizeX;

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void MapSizeZ_WhenWorldNull_ReturnsZero()
    {
        // Arrange
        var api = Substitute.For<ICoreServerAPI>();
        var logger = Substitute.For<ILogger>();
        api.World.Returns((IServerWorldAccessor?)null);

        var service = new MapDataExtractionService(api, logger);

        // Act
        var result = service.MapSizeZ;

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void IsAvailable_WhenNotInitialized_ReturnsFalse()
    {
        // Arrange
        var api = Substitute.For<ICoreServerAPI>();
        var logger = Substitute.For<ILogger>();
        api.World.Returns((IServerWorldAccessor?)null);

        var service = new MapDataExtractionService(api, logger);

        // Act & Assert
        service.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void ChunkSize_MatchesGlobalConstant()
    {
        // Arrange
        var api = Substitute.For<ICoreServerAPI>();
        var logger = Substitute.For<ILogger>();
        var service = new MapDataExtractionService(api, logger);

        // Act & Assert - GlobalConstants.ChunkSize is 32
        service.ChunkSize.Should().Be(32);
        MapDataExtractionService.ChunkSizeConst.Should().Be(32);
    }

    [Fact]
    public void MapSizeX_WhenBlockAccessorAvailable_ReturnsMapSize()
    {
        // Arrange
        var api = Substitute.For<ICoreServerAPI>();
        var logger = Substitute.For<ILogger>();
        var world = Substitute.For<IServerWorldAccessor>();
        var blockAccessor = Substitute.For<IBlockAccessor>();

        blockAccessor.MapSizeX.Returns(1024);
        world.BlockAccessor.Returns(blockAccessor);
        api.World.Returns(world);

        var service = new MapDataExtractionService(api, logger);

        // Act & Assert
        service.MapSizeX.Should().Be(1024);
    }

    [Fact]
    public void MapSizeZ_WhenBlockAccessorAvailable_ReturnsMapSize()
    {
        // Arrange
        var api = Substitute.For<ICoreServerAPI>();
        var logger = Substitute.For<ILogger>();
        var world = Substitute.For<IServerWorldAccessor>();
        var blockAccessor = Substitute.For<IBlockAccessor>();

        blockAccessor.MapSizeZ.Returns(2048);
        world.BlockAccessor.Returns(blockAccessor);
        api.World.Returns(world);

        var service = new MapDataExtractionService(api, logger);

        // Act & Assert
        service.MapSizeZ.Should().Be(2048);
    }

    [Fact]
    public void SpawnX_WhenDefaultSpawnPositionSet_ReturnsSpawnX()
    {
        // Arrange
        var api = Substitute.For<ICoreServerAPI>();
        var logger = Substitute.For<ILogger>();
        var world = Substitute.For<IServerWorldAccessor>();
        var spawnPos = new EntityPos(512.5, 100, 256.5);

        world.DefaultSpawnPosition.Returns(spawnPos);
        api.World.Returns(world);

        var service = new MapDataExtractionService(api, logger);

        // Act & Assert
        service.SpawnX.Should().Be(512);
    }

    [Fact]
    public void SpawnZ_WhenDefaultSpawnPositionSet_ReturnsSpawnZ()
    {
        // Arrange
        var api = Substitute.For<ICoreServerAPI>();
        var logger = Substitute.For<ILogger>();
        var world = Substitute.For<IServerWorldAccessor>();
        var spawnPos = new EntityPos(512.5, 100, 256.5);

        world.DefaultSpawnPosition.Returns(spawnPos);
        api.World.Returns(world);

        var service = new MapDataExtractionService(api, logger);

        // Act & Assert
        service.SpawnZ.Should().Be(256);
    }

    [Fact]
    public void SpawnX_WhenDefaultSpawnPositionNull_ReturnsCenterOfMap()
    {
        // Arrange
        var api = Substitute.For<ICoreServerAPI>();
        var logger = Substitute.For<ILogger>();
        var world = Substitute.For<IServerWorldAccessor>();
        var blockAccessor = Substitute.For<IBlockAccessor>();

        world.DefaultSpawnPosition.ReturnsNull();
        blockAccessor.MapSizeX.Returns(1024);
        world.BlockAccessor.Returns(blockAccessor);
        api.World.Returns(world);

        var service = new MapDataExtractionService(api, logger);

        // Act & Assert - should return center when spawn not set
        service.SpawnX.Should().Be(512);
    }

    #region Hash Calculation Tests

    [Fact]
    public void CalculateContentHash_EmptyData_ReturnsConsistentHash()
    {
        // Arrange
        var heightMap = new ushort[1024];
        var blockIds = new int[1024];

        // Act
        var hash1 = MapDataExtractionService.CalculateContentHash(heightMap, blockIds);
        var hash2 = MapDataExtractionService.CalculateContentHash(heightMap, blockIds);

        // Assert
        hash1.Should().NotBeNullOrEmpty();
        hash1.Should().Be(hash2); // Should be deterministic
    }

    [Fact]
    public void CalculateContentHash_DifferentHeightMaps_ReturnsDifferentHashes()
    {
        // Arrange
        var heightMap1 = new ushort[1024];
        var heightMap2 = new ushort[1024];
        heightMap2[0] = 100; // Different

        var blockIds = new int[1024];

        // Act
        var hash1 = MapDataExtractionService.CalculateContentHash(heightMap1, blockIds);
        var hash2 = MapDataExtractionService.CalculateContentHash(heightMap2, blockIds);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void CalculateContentHash_DifferentBlockIds_ReturnsDifferentHashes()
    {
        // Arrange
        var heightMap = new ushort[1024];
        var blockIds1 = new int[1024];
        var blockIds2 = new int[1024];
        blockIds2[0] = 42; // Different

        // Act
        var hash1 = MapDataExtractionService.CalculateContentHash(heightMap, blockIds1);
        var hash2 = MapDataExtractionService.CalculateContentHash(heightMap, blockIds2);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void CalculateContentHash_ReturnsValidSha256HexString()
    {
        // Arrange
        var heightMap = new ushort[1024];
        var blockIds = new int[1024];

        // Act
        var hash = MapDataExtractionService.CalculateContentHash(heightMap, blockIds);

        // Assert - SHA256 produces 64 hex characters
        hash.Should().HaveLength(64);
        hash.Should().MatchRegex("^[A-F0-9]+$"); // All uppercase hex
    }

    [Fact]
    public void CalculateContentHash_RealisticData_ProducesHash()
    {
        // Arrange - simulate realistic terrain data
        var heightMap = new ushort[1024];
        var blockIds = new int[1024];
        var random = new Random(42);

        for (var i = 0; i < 1024; i++)
        {
            heightMap[i] = (ushort)random.Next(50, 150);
            blockIds[i] = random.Next(1, 1000);
        }

        // Act
        var hash = MapDataExtractionService.CalculateContentHash(heightMap, blockIds);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().HaveLength(64);
    }

    #endregion

    // Note: GetBlockColorCode tests have been removed because Block is a concrete class
    // with non-virtual properties that cannot be mocked with NSubstitute.
    // The GetBlockColorCode logic is tested indirectly through integration tests.

    #region Async Method Tests

    [Fact]
    public async Task ExtractChunkDataAsync_WhenNotAvailable_ReturnsNull()
    {
        // Arrange
        var api = Substitute.For<ICoreServerAPI>();
        var logger = Substitute.For<ILogger>();
        api.World.Returns((IServerWorldAccessor?)null);

        var service = new MapDataExtractionService(api, logger);

        // Act
        var result = await service.ExtractChunkDataAsync(0, 0);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExtractChunkHashesAsync_WhenNotAvailable_ReturnsEmptyList()
    {
        // Arrange
        var api = Substitute.For<ICoreServerAPI>();
        var logger = Substitute.For<ILogger>();
        api.World.Returns((IServerWorldAccessor?)null);

        var service = new MapDataExtractionService(api, logger);

        // Act
        var result = await service.ExtractChunkHashesAsync(0, 0, 10);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion
}
