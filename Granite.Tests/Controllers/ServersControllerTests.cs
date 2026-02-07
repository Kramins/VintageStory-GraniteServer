using System;
using System.Threading.Tasks;
using FluentAssertions;
using Granite.Common.Dto;
using Granite.Common.Dto.JsonApi;
using Granite.Server.Controllers;
using Granite.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Granite.Tests.Controllers;

public class ServersControllerTests
{
    private readonly ILogger<ServersController> _mockLogger;
    private readonly ServersService _mockService;
    private readonly ServersController _controller;

    public ServersControllerTests()
    {
        _mockLogger = Substitute.For<ILogger<ServersController>>();
        _mockService = Substitute.For<ServersService>();
        _controller = new ServersController(_mockLogger, _mockService);

        // Setup mock HttpContext
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    #region GetServers Tests

    [Fact]
    public async Task GetServers_ReturnsOkWithServerList()
    {
        // Arrange
        var servers = new List<ServerDTO>
        {
            new() { Id = Guid.NewGuid(), Name = "Server 1", CreatedAt = DateTime.UtcNow, IsOnline = true },
            new() { Id = Guid.NewGuid(), Name = "Server 2", CreatedAt = DateTime.UtcNow, IsOnline = false }
        };
        _mockService.GetServersAsync().Returns(servers);

        // Act
        var result = await _controller.GetServers();

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(StatusCodes.Status200OK);

        var jsonApiDoc = okResult.Value as JsonApiDocument<List<ServerDTO>>;
        jsonApiDoc.Should().NotBeNull();
        jsonApiDoc!.Data.Should().NotBeNull();
        jsonApiDoc.Data!.Should().HaveCount(2);
    }

    #endregion

    #region GetServerById Tests

    [Fact]
    public async Task GetServerById_ExistingServer_ReturnsOkWithServer()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var server = new ServerDTO
        {
            Id = serverId,
            Name = "Test Server",
            Description = "Test Description",
            CreatedAt = DateTime.UtcNow,
            IsOnline = true
        };
        _mockService.GetServerByIdAsync(serverId).Returns(server);

        // Act
        var result = await _controller.GetServerById(serverId);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(StatusCodes.Status200OK);

        var jsonApiDoc = okResult.Value as JsonApiDocument<ServerDTO>;
        jsonApiDoc.Should().NotBeNull();
        jsonApiDoc!.Data.Should().NotBeNull();
        jsonApiDoc.Data!.Id.Should().Be(serverId);
        jsonApiDoc.Data.Name.Should().Be("Test Server");
    }

    [Fact]
    public async Task GetServerById_NonExistentServer_ReturnsNotFound()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        _mockService.GetServerByIdAsync(serverId).Returns((ServerDTO?)null);

        // Act
        var result = await _controller.GetServerById(serverId);

        // Assert
        result.Should().NotBeNull();
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.StatusCode.Should().Be(StatusCodes.Status404NotFound);

        var jsonApiDoc = notFoundResult.Value as JsonApiDocument<object>;
        jsonApiDoc.Should().NotBeNull();
        jsonApiDoc!.Data.Should().BeNull();
        jsonApiDoc.Errors.Should().NotBeEmpty();
        jsonApiDoc.Errors[0].Code.Should().Be("Server not found");
    }

    #endregion

    #region CreateServer Tests

    [Fact]
    public async Task CreateServer_ValidRequest_ReturnsCreatedWithAccessToken()
    {
        // Arrange
        var request = new CreateServerRequestDTO
        {
            Name = "New Server",
            Description = "New Description"
        };
        var createdServer = new ServerCreatedResponseDTO
        {
            Id = Guid.NewGuid(),
            Name = "New Server",
            Description = "New Description",
            CreatedAt = DateTime.UtcNow,
            IsOnline = false,
            AccessToken = "generated-secure-token-12345"
        };
        _mockService.CreateServerAsync(request).Returns(createdServer);

        // Act
        var result = await _controller.CreateServer(request);

        // Assert
        result.Should().NotBeNull();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult.Should().NotBeNull();
        createdResult!.StatusCode.Should().Be(StatusCodes.Status201Created);
        createdResult.ActionName.Should().Be(nameof(ServersController.GetServerById));

        var jsonApiDoc = createdResult.Value as JsonApiDocument<ServerCreatedResponseDTO>;
        jsonApiDoc.Should().NotBeNull();
        jsonApiDoc!.Data.Should().NotBeNull();
        jsonApiDoc.Data!.Name.Should().Be("New Server");
        jsonApiDoc.Data.AccessToken.Should().Be("generated-secure-token-12345");
    }

    [Fact]
    public async Task CreateServer_EmptyName_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateServerRequestDTO
        {
            Name = "",
            Description = "Description"
        };

        // Act
        var result = await _controller.CreateServer(request);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        var jsonApiDoc = badRequestResult.Value as JsonApiDocument<object>;
        jsonApiDoc.Should().NotBeNull();
        jsonApiDoc!.Errors.Should().NotBeEmpty();
        jsonApiDoc.Errors[0].Code.Should().Be("Invalid request");
    }

    [Fact]
    public async Task CreateServer_WhitespaceName_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateServerRequestDTO
        {
            Name = "   ",
            Description = "Description"
        };

        // Act
        var result = await _controller.CreateServer(request);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task CreateServer_DuplicateName_ReturnsConflict()
    {
        // Arrange
        var request = new CreateServerRequestDTO
        {
            Name = "Existing Server",
            Description = "Description"
        };
        _mockService.CreateServerAsync(request)
            .ThrowsAsync(new InvalidOperationException("A server with the name 'Existing Server' already exists."));

        // Act
        var result = await _controller.CreateServer(request);

        // Assert
        result.Should().NotBeNull();
        var conflictResult = result.Result as ConflictObjectResult;
        conflictResult.Should().NotBeNull();
        conflictResult!.StatusCode.Should().Be(StatusCodes.Status409Conflict);

        var jsonApiDoc = conflictResult.Value as JsonApiDocument<object>;
        jsonApiDoc.Should().NotBeNull();
        jsonApiDoc!.Errors.Should().NotBeEmpty();
        jsonApiDoc.Errors[0].Code.Should().Be("Server name conflict");
    }

    #endregion

    #region UpdateServer Tests

    [Fact]
    public async Task UpdateServer_ValidRequest_ReturnsOkWithUpdatedServer()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var request = new UpdateServerRequestDTO
        {
            Name = "Updated Server",
            Description = "Updated Description"
        };
        var updatedServer = new ServerDTO
        {
            Id = serverId,
            Name = "Updated Server",
            Description = "Updated Description",
            CreatedAt = DateTime.UtcNow,
            IsOnline = true
        };
        _mockService.UpdateServerAsync(serverId, request).Returns(updatedServer);

        // Act
        var result = await _controller.UpdateServer(serverId, request);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(StatusCodes.Status200OK);

        var jsonApiDoc = okResult.Value as JsonApiDocument<ServerDTO>;
        jsonApiDoc.Should().NotBeNull();
        jsonApiDoc!.Data.Should().NotBeNull();
        jsonApiDoc.Data!.Name.Should().Be("Updated Server");
    }

    [Fact]
    public async Task UpdateServer_EmptyName_ReturnsBadRequest()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var request = new UpdateServerRequestDTO
        {
            Name = "",
            Description = "Description"
        };

        // Act
        var result = await _controller.UpdateServer(serverId, request);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task UpdateServer_NonExistentServer_ReturnsNotFound()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var request = new UpdateServerRequestDTO
        {
            Name = "Updated Server",
            Description = "Description"
        };
        _mockService.UpdateServerAsync(serverId, request).Returns((ServerDTO?)null);

        // Act
        var result = await _controller.UpdateServer(serverId, request);

        // Assert
        result.Should().NotBeNull();
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.StatusCode.Should().Be(StatusCodes.Status404NotFound);

        var jsonApiDoc = notFoundResult.Value as JsonApiDocument<object>;
        jsonApiDoc.Should().NotBeNull();
        jsonApiDoc!.Errors.Should().NotBeEmpty();
        jsonApiDoc.Errors[0].Code.Should().Be("Server not found");
    }

    [Fact]
    public async Task UpdateServer_DuplicateName_ReturnsConflict()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var request = new UpdateServerRequestDTO
        {
            Name = "Existing Server",
            Description = "Description"
        };
        _mockService.UpdateServerAsync(serverId, request)
            .ThrowsAsync(new InvalidOperationException("A server with the name 'Existing Server' already exists."));

        // Act
        var result = await _controller.UpdateServer(serverId, request);

        // Assert
        result.Should().NotBeNull();
        var conflictResult = result.Result as ConflictObjectResult;
        conflictResult.Should().NotBeNull();
        conflictResult!.StatusCode.Should().Be(StatusCodes.Status409Conflict);

        var jsonApiDoc = conflictResult.Value as JsonApiDocument<object>;
        jsonApiDoc.Should().NotBeNull();
        jsonApiDoc!.Errors.Should().NotBeEmpty();
        jsonApiDoc.Errors[0].Code.Should().Be("Server name conflict");
    }

    #endregion

    #region DeleteServer Tests

    [Fact]
    public async Task DeleteServer_ExistingServer_ReturnsNoContent()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        _mockService.DeleteServerAsync(serverId).Returns(true);

        // Act
        var result = await _controller.DeleteServer(serverId);

        // Assert
        result.Should().NotBeNull();
        var noContentResult = result as NoContentResult;
        noContentResult.Should().NotBeNull();
        noContentResult!.StatusCode.Should().Be(StatusCodes.Status204NoContent);
    }

    [Fact]
    public async Task DeleteServer_NonExistentServer_ReturnsNotFound()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        _mockService.DeleteServerAsync(serverId).Returns(false);

        // Act
        var result = await _controller.DeleteServer(serverId);

        // Assert
        result.Should().NotBeNull();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.StatusCode.Should().Be(StatusCodes.Status404NotFound);

        var jsonApiDoc = notFoundResult.Value as JsonApiDocument<object>;
        jsonApiDoc.Should().NotBeNull();
        jsonApiDoc!.Errors.Should().NotBeEmpty();
        jsonApiDoc.Errors[0].Code.Should().Be("Server not found");
    }

    #endregion

    #region RegenerateAccessToken Tests

    [Fact]
    public async Task RegenerateAccessToken_ExistingServer_ReturnsOkWithNewToken()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var tokenResponse = new TokenRegeneratedResponseDTO
        {
            Id = serverId,
            AccessToken = "new-generated-token-67890"
        };
        _mockService.RegenerateAccessTokenAsync(serverId).Returns(tokenResponse);

        // Act
        var result = await _controller.RegenerateAccessToken(serverId);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(StatusCodes.Status200OK);

        var jsonApiDoc = okResult.Value as JsonApiDocument<TokenRegeneratedResponseDTO>;
        jsonApiDoc.Should().NotBeNull();
        jsonApiDoc!.Data.Should().NotBeNull();
        jsonApiDoc.Data!.Id.Should().Be(serverId);
        jsonApiDoc.Data.AccessToken.Should().Be("new-generated-token-67890");
    }

    [Fact]
    public async Task RegenerateAccessToken_NonExistentServer_ReturnsNotFound()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        _mockService.RegenerateAccessTokenAsync(serverId).Returns((TokenRegeneratedResponseDTO?)null);

        // Act
        var result = await _controller.RegenerateAccessToken(serverId);

        // Assert
        result.Should().NotBeNull();
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.StatusCode.Should().Be(StatusCodes.Status404NotFound);

        var jsonApiDoc = notFoundResult.Value as JsonApiDocument<object>;
        jsonApiDoc.Should().NotBeNull();
        jsonApiDoc!.Errors.Should().NotBeEmpty();
        jsonApiDoc.Errors[0].Code.Should().Be("Server not found");
    }

    #endregion
}
