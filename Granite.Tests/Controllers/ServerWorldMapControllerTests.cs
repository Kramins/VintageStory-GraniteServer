using FluentAssertions;
using Granite.Common.Dto;
using Granite.Common.Dto.JsonApi;
using Granite.Server.Controllers;
using Granite.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Granite.Tests.Controllers;

public class ServerWorldMapControllerTests
{
    private readonly IServerWorldMapService _mockService;
    private readonly ILogger<ServerWorldMapController> _mockLogger;
    private readonly ServerWorldMapController _controller;
    private readonly Guid _testServerId = Guid.NewGuid();

    public ServerWorldMapControllerTests()
    {
        _mockService = Substitute.For<IServerWorldMapService>();
        _mockLogger = Substitute.For<ILogger<ServerWorldMapController>>();
        _controller = new ServerWorldMapController(_mockService, _mockLogger);

        // Setup mock HttpContext with response headers
        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    #region GetWorldBounds Tests

    [Fact]
    public async Task GetWorldBounds_ValidServerId_ReturnsOkWithBounds()
    {
        // Arrange
        var expectedBounds = new WorldMapBoundsDTO
        {
            MinChunkX = -10,
            MaxChunkX = 20,
            MinChunkZ = -5,
            MaxChunkZ = 15,
            TotalChunks = 100,
        };

        _mockService.GetWorldBoundsAsync(_testServerId).Returns(expectedBounds);

        // Act
        var result = await _controller.GetWorldBounds(_testServerId);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var jsonApiDoc = okResult
            .Value.Should()
            .BeOfType<JsonApiDocument<WorldMapBoundsDTO>>()
            .Subject;
        jsonApiDoc.Data.Should().BeEquivalentTo(expectedBounds);
    }

    [Fact]
    public async Task GetWorldBounds_NoMapData_ReturnsNotFound()
    {
        // Arrange
        _mockService.GetWorldBoundsAsync(_testServerId).Returns((WorldMapBoundsDTO?)null);

        // Act
        var result = await _controller.GetWorldBounds(_testServerId);

        // Assert
        result.Should().NotBeNull();
        var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var jsonApiDoc = notFoundResult
            .Value.Should()
            .BeOfType<JsonApiDocument<WorldMapBoundsDTO>>()
            .Subject;
        jsonApiDoc.Errors.Should().ContainSingle();
        jsonApiDoc.Errors[0].Code.Should().Be("404");
        jsonApiDoc.Errors[0].Message.Should().Contain("No map data found");
    }

    [Fact]
    public async Task GetWorldBounds_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        _mockService
            .GetWorldBoundsAsync(_testServerId)
            .Returns<WorldMapBoundsDTO?>(_ => throw new Exception("Database error"));

        // Act
        var result = await _controller.GetWorldBounds(_testServerId);

        // Assert
        result.Should().NotBeNull();
        var errorResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        errorResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        var jsonApiDoc = errorResult
            .Value.Should()
            .BeOfType<JsonApiDocument<WorldMapBoundsDTO>>()
            .Subject;
        jsonApiDoc.Errors.Should().ContainSingle();
        jsonApiDoc.Errors[0].Code.Should().Be("500");
    }

    #endregion

    #region GetGroupedTileImage Tests - Coordinate Transformation

    [Theory]
    [InlineData(0, 0, 0, 0)] // Origin
    [InlineData(5, -10, 5, 10)] // Positive X, negative Y -> positive Z
    [InlineData(-3, 7, -3, -7)] // Negative X, positive Y -> negative Z
    [InlineData(100, -200, 100, 200)] // Large values
    public async Task GetGroupedTileImage_CoordinateTransformation_ConvertsCorrectly(
        int x,
        int y,
        int expectedChunkX,
        int expectedChunkZ
    )
    {
        // Arrange
        var mockImageData = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header
        _mockService
            .GetGroupedTileImageAsync(_testServerId, expectedChunkX, expectedChunkZ)
            .Returns(mockImageData);

        // Act
        var result = await _controller.GetGroupedTileImage(_testServerId, x, y);

        // Assert
        result.Should().BeOfType<FileContentResult>();
        var fileResult = (FileContentResult)result;
        fileResult.FileContents.Should().BeEquivalentTo(mockImageData);
        fileResult.ContentType.Should().Be("image/png");

        // Verify the service was called with correct converted coordinates
        await _mockService
            .Received(1)
            .GetGroupedTileImageAsync(
                _testServerId,
                Arg.Is<int>(cx => cx == expectedChunkX),
                Arg.Is<int>(cz => cz == expectedChunkZ)
            );
    }

    [Fact]
    public async Task GetGroupedTileImage_ValidTile_ReturnsFileWithCacheHeaders()
    {
        // Arrange
        var x = 5;
        var y = -10;
        var mockImageData = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var mockMetadata = new MapTileMetadataDTO
        {
            ChunkX = 5,
            ChunkZ = 10,
            ChunkHash = "abc123hash",
            Width = 32,
            Height = 32,
            ExtractedAt = DateTime.UtcNow,
        };

        _mockService.GetGroupedTileImageAsync(_testServerId, 5, 10).Returns(mockImageData);
        _mockService.GetTileMetadataAsync(_testServerId, 5, 10).Returns(mockMetadata);

        // Act
        var result = await _controller.GetGroupedTileImage(_testServerId, x, y);

        // Assert
        result.Should().BeOfType<FileContentResult>();
        var fileResult = (FileContentResult)result;
        fileResult.ContentType.Should().Be("image/png");

        // Verify cache headers were set
        _controller.Response.Headers.Should().ContainKey("ETag");
        _controller.Response.Headers.Should().ContainKey("Cache-Control");
        _controller.Response.Headers["ETag"].ToString().Should().Contain(mockMetadata.ChunkHash);
        _controller.Response.Headers["Cache-Control"].ToString().Should().Contain("public");
    }

    [Fact]
    public async Task GetGroupedTileImage_TileNotFound_ReturnsNotFound()
    {
        // Arrange
        var x = 999;
        var y = -999;
        _mockService.GetGroupedTileImageAsync(_testServerId, 999, 999).Returns((byte[]?)null);

        // Act
        var result = await _controller.GetGroupedTileImage(_testServerId, x, y);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = (NotFoundObjectResult)result;
        var jsonApiDoc = notFoundResult.Value.Should().BeOfType<JsonApiDocument<object>>().Subject;
        jsonApiDoc.Errors.Should().ContainSingle();
        jsonApiDoc.Errors[0].Code.Should().Be("404");
        jsonApiDoc.Errors[0].Message.Should().Contain($"x={x}");
        jsonApiDoc.Errors[0].Message.Should().Contain($"y={y}");
    }

    [Fact]
    public async Task GetGroupedTileImage_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var x = 1;
        var y = -1;
        _mockService
            .GetGroupedTileImageAsync(_testServerId, Arg.Any<int>(), Arg.Any<int>())
            .Returns<byte[]?>(_ => throw new Exception("Rendering error"));

        // Act
        var result = await _controller.GetGroupedTileImage(_testServerId, x, y);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var errorResult = (ObjectResult)result;
        errorResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    #endregion

    #region GetTileMetadata Tests - Coordinate Transformation

    [Theory]
    [InlineData(0, 0, 0, 0)] // Origin
    [InlineData(10, -20, 10, 20)] // Positive X, negative Y -> positive Z
    [InlineData(-5, 15, -5, -15)] // Negative X, positive Y -> negative Z
    public async Task GetTileMetadata_CoordinateTransformation_ConvertsCorrectly(
        int x,
        int y,
        int expectedChunkX,
        int expectedChunkZ
    )
    {
        // Arrange
        var mockMetadata = new MapTileMetadataDTO
        {
            ChunkX = expectedChunkX,
            ChunkZ = expectedChunkZ,
            ChunkHash = "test-hash",
            Width = 32,
            Height = 32,
            ExtractedAt = DateTime.UtcNow,
        };

        _mockService
            .GetTileMetadataAsync(_testServerId, expectedChunkX, expectedChunkZ)
            .Returns(mockMetadata);

        // Act
        var result = await _controller.GetTileMetadata(_testServerId, x, y);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var jsonApiDoc = okResult
            .Value.Should()
            .BeOfType<JsonApiDocument<MapTileMetadataDTO>>()
            .Subject;
        jsonApiDoc.Data.Should().BeEquivalentTo(mockMetadata);

        // Verify the service was called with correct converted coordinates
        await _mockService
            .Received(1)
            .GetTileMetadataAsync(
                _testServerId,
                Arg.Is<int>(cx => cx == expectedChunkX),
                Arg.Is<int>(cz => cz == expectedChunkZ)
            );
    }

    [Fact]
    public async Task GetTileMetadata_ValidTile_ReturnsMetadata()
    {
        // Arrange
        var x = 7;
        var y = -3;
        var expectedMetadata = new MapTileMetadataDTO
        {
            ChunkX = 7,
            ChunkZ = 3,
            ChunkHash = "xyz789",
            Width = 32,
            Height = 32,
            ExtractedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
        };

        _mockService.GetTileMetadataAsync(_testServerId, 7, 3).Returns(expectedMetadata);

        // Act
        var result = await _controller.GetTileMetadata(_testServerId, x, y);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var jsonApiDoc = okResult
            .Value.Should()
            .BeOfType<JsonApiDocument<MapTileMetadataDTO>>()
            .Subject;
        jsonApiDoc.Data.Should().NotBeNull();
        jsonApiDoc.Data!.ChunkX.Should().Be(7);
        jsonApiDoc.Data.ChunkZ.Should().Be(3);
        jsonApiDoc.Data.ChunkHash.Should().Be("xyz789");
        jsonApiDoc.Data.Width.Should().Be(32);
        jsonApiDoc.Data.Height.Should().Be(32);
    }

    [Fact]
    public async Task GetTileMetadata_TileNotFound_ReturnsNotFound()
    {
        // Arrange
        var x = 404;
        var y = -404;
        _mockService
            .GetTileMetadataAsync(_testServerId, 404, 404)
            .Returns((MapTileMetadataDTO?)null);

        // Act
        var result = await _controller.GetTileMetadata(_testServerId, x, y);

        // Assert
        result.Should().NotBeNull();
        var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var jsonApiDoc = notFoundResult
            .Value.Should()
            .BeOfType<JsonApiDocument<MapTileMetadataDTO>>()
            .Subject;
        jsonApiDoc.Errors.Should().ContainSingle();
        jsonApiDoc.Errors[0].Code.Should().Be("404");
        jsonApiDoc.Errors[0].Message.Should().Contain($"x={x}");
        jsonApiDoc.Errors[0].Message.Should().Contain($"y={y}");
    }

    [Fact]
    public async Task GetTileMetadata_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var x = 1;
        var y = -1;
        _mockService
            .GetTileMetadataAsync(_testServerId, Arg.Any<int>(), Arg.Any<int>())
            .Returns<MapTileMetadataDTO?>(_ => throw new Exception("Database error"));

        // Act
        var result = await _controller.GetTileMetadata(_testServerId, x, y);

        // Assert
        result.Should().NotBeNull();
        var errorResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        errorResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        var jsonApiDoc = errorResult
            .Value.Should()
            .BeOfType<JsonApiDocument<MapTileMetadataDTO>>()
            .Subject;
        jsonApiDoc.Errors.Should().ContainSingle();
        jsonApiDoc.Errors[0].Code.Should().Be("500");
    }

    #endregion

    #region Edge Cases and Boundary Tests

    [Theory]
    [InlineData(int.MaxValue, int.MinValue)] // Extreme values
    [InlineData(-1000000, 1000000)] // Very large negative/positive
    public async Task GetGroupedTileImage_ExtremeCoordinates_HandlesGracefully(int x, int y)
    {
        // Arrange
        var expectedChunkZ = -y;
        _mockService
            .GetGroupedTileImageAsync(_testServerId, x, expectedChunkZ)
            .Returns((byte[]?)null);

        // Act
        var result = await _controller.GetGroupedTileImage(_testServerId, x, y);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        await _mockService.Received(1).GetGroupedTileImageAsync(_testServerId, x, expectedChunkZ);
    }

    #endregion
}
