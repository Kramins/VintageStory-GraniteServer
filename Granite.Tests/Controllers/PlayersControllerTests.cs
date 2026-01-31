using FluentAssertions;
using Granite.Common.Dto;
using Granite.Common.Dto.JsonApi;
using Granite.Server.Controllers;
using Granite.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Granite.Tests.Controllers;

public class PlayersControllerTests
{
    private readonly IPlayersService _mockService;
    private readonly PlayersController _controller;

    public PlayersControllerTests()
    {
        _mockService = Substitute.For<IPlayersService>();
        _controller = new PlayersController(_mockService);

        // Setup mock HttpContext
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task FindPlayerByName_ValidPlayerName_ReturnsOkWithPlayerData()
    {
        // Arrange
        var playerName = "TestPlayer";
        var expectedUid = "player-uid-12345";
        var expectedPlayer = new PlayerNameIdDTO
        {
            Id = expectedUid,
            Name = playerName
        };

        _mockService
            .FindPlayerByNameAsync(playerName, Arg.Any<CancellationToken>())
            .Returns(expectedPlayer);

        // Act
        var result = await _controller.FindPlayerByName(playerName);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(StatusCodes.Status200OK);

        var jsonApiDoc = okResult.Value as JsonApiDocument<PlayerNameIdDTO>;
        jsonApiDoc.Should().NotBeNull();
        jsonApiDoc!.Data.Should().NotBeNull();
        jsonApiDoc.Data!.Id.Should().Be(expectedUid);
        jsonApiDoc.Data.Name.Should().Be(playerName);
        jsonApiDoc.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task FindPlayerByName_PlayerNotFound_ReturnsNotFound()
    {
        // Arrange
        var playerName = "NonExistentPlayer";

        _mockService
            .FindPlayerByNameAsync(playerName, Arg.Any<CancellationToken>())
            .Returns((PlayerNameIdDTO?)null);

        // Act
        var result = await _controller.FindPlayerByName(playerName);

        // Assert
        result.Should().NotBeNull();
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.StatusCode.Should().Be(StatusCodes.Status404NotFound);

        var jsonApiDoc = notFoundResult.Value as JsonApiDocument<PlayerNameIdDTO>;
        jsonApiDoc.Should().NotBeNull();
        jsonApiDoc!.Data.Should().BeNull();
        jsonApiDoc.Errors.Should().NotBeEmpty();
        jsonApiDoc.Errors.Should().ContainSingle();
        jsonApiDoc.Errors[0].Code.Should().Be("NOT_FOUND");
        jsonApiDoc.Errors[0].Message.Should().Contain(playerName);
    }

    [Fact]
    public async Task FindPlayerByName_EmptyPlayerName_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.FindPlayerByName(string.Empty);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        var jsonApiDoc = badRequestResult.Value as JsonApiDocument<PlayerNameIdDTO>;
        jsonApiDoc.Should().NotBeNull();
        jsonApiDoc!.Data.Should().BeNull();
        jsonApiDoc.Errors.Should().NotBeEmpty();
        jsonApiDoc.Errors.Should().ContainSingle();
        jsonApiDoc.Errors[0].Code.Should().Be("INVALID_REQUEST");
        jsonApiDoc.Errors[0].Message.Should().Contain("required");
    }

    [Fact]
    public async Task FindPlayerByName_NullPlayerName_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.FindPlayerByName(null!);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        var jsonApiDoc = badRequestResult.Value as JsonApiDocument<PlayerNameIdDTO>;
        jsonApiDoc.Should().NotBeNull();
        jsonApiDoc!.Data.Should().BeNull();
        jsonApiDoc.Errors.Should().NotBeEmpty();
        jsonApiDoc.Errors.Should().ContainSingle();
        jsonApiDoc.Errors[0].Code.Should().Be("INVALID_REQUEST");
        jsonApiDoc.Errors[0].Message.Should().Contain("required");
    }

    [Fact]
    public async Task FindPlayerByName_WhitespacePlayerName_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.FindPlayerByName("   ");

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

        var jsonApiDoc = badRequestResult.Value as JsonApiDocument<PlayerNameIdDTO>;
        jsonApiDoc.Should().NotBeNull();
        jsonApiDoc!.Errors.Should().NotBeEmpty();
        jsonApiDoc.Errors[0].Code.Should().Be("INVALID_REQUEST");
    }

    [Fact]
    public async Task FindPlayerByName_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var playerName = "TestPlayer";
        var exception = new HttpRequestException("Network error");

        _mockService
            .When(x => x.FindPlayerByNameAsync(playerName, Arg.Any<CancellationToken>()))
            .Do(x => throw exception);

        // Act
        var result = await _controller.FindPlayerByName(playerName);

        // Assert
        result.Should().NotBeNull();
        var statusResult = result.Result as ObjectResult;
        statusResult.Should().NotBeNull();
        statusResult!.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);

        var jsonApiDoc = statusResult.Value as JsonApiDocument<PlayerNameIdDTO>;
        jsonApiDoc.Should().NotBeNull();
        jsonApiDoc!.Data.Should().BeNull();
        jsonApiDoc.Errors.Should().NotBeEmpty();
        jsonApiDoc.Errors[0].Code.Should().Be("SERVER_ERROR");
        jsonApiDoc.Errors[0].Message.Should().Contain("Failed to resolve player name");
    }

    [Fact]
    public async Task FindPlayerByName_PlayerNameWithSpecialCharacters_ReturnsOk()
    {
        // Arrange
        var playerName = "Test.Player-123_New";
        var expectedUid = "uid-with-special-chars";
        var expectedPlayer = new PlayerNameIdDTO
        {
            Id = expectedUid,
            Name = playerName
        };

        _mockService
            .FindPlayerByNameAsync(playerName, Arg.Any<CancellationToken>())
            .Returns(expectedPlayer);

        // Act
        var result = await _controller.FindPlayerByName(playerName);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();

        var jsonApiDoc = okResult!.Value as JsonApiDocument<PlayerNameIdDTO>;
        jsonApiDoc!.Data!.Name.Should().Be(playerName);
        jsonApiDoc.Data.Id.Should().Be(expectedUid);
    }

    [Fact]
    public async Task FindPlayerByName_PlayerNameWithSpaces_ReturnsOk()
    {
        // Arrange
        var playerName = "Test Player Name";
        var expectedUid = "uid-with-spaces";
        var expectedPlayer = new PlayerNameIdDTO
        {
            Id = expectedUid,
            Name = playerName
        };

        _mockService
            .FindPlayerByNameAsync(playerName, Arg.Any<CancellationToken>())
            .Returns(expectedPlayer);

        // Act
        var result = await _controller.FindPlayerByName(playerName);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();

        var jsonApiDoc = okResult!.Value as JsonApiDocument<PlayerNameIdDTO>;
        jsonApiDoc!.Data!.Name.Should().Be(playerName);
    }

    [Fact]
    public async Task FindPlayerByName_LongPlayerName_ReturnsOk()
    {
        // Arrange - Test with a very long player name
        var playerName = new string('A', 255);
        var expectedUid = "uid-long-name";
        var expectedPlayer = new PlayerNameIdDTO
        {
            Id = expectedUid,
            Name = playerName
        };

        _mockService
            .FindPlayerByNameAsync(playerName, Arg.Any<CancellationToken>())
            .Returns(expectedPlayer);

        // Act
        var result = await _controller.FindPlayerByName(playerName);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
    }

    [Fact]
    public async Task FindPlayerByName_CaseInsensitiveSearch_ReturnsOk()
    {
        // Arrange
        var playerName = "testplayer";
        var expectedUid = "uid-case-test";
        var expectedPlayer = new PlayerNameIdDTO
        {
            Id = expectedUid,
            Name = playerName
        };

        _mockService
            .FindPlayerByNameAsync(playerName, Arg.Any<CancellationToken>())
            .Returns(expectedPlayer);

        // Act
        var result = await _controller.FindPlayerByName(playerName);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
    }

    [Fact]
    public async Task FindPlayerByName_PassesCancellationToken_ToService()
    {
        // Arrange
        var playerName = "TestPlayer";
        var expectedPlayer = new PlayerNameIdDTO
        {
            Id = "test-uid",
            Name = playerName
        };

        _mockService
            .FindPlayerByNameAsync(playerName, Arg.Any<CancellationToken>())
            .Returns(expectedPlayer);

        // Act
        var result = await _controller.FindPlayerByName(playerName);

        // Assert
        await _mockService.Received(1).FindPlayerByNameAsync(playerName, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FindPlayerByName_MultipleConsecutiveCalls_ReturnCorrectResults()
    {
        // Arrange
        var player1Name = "Player1";
        var player1 = new PlayerNameIdDTO { Id = "uid-1", Name = player1Name };
        var player2Name = "Player2";
        var player2 = new PlayerNameIdDTO { Id = "uid-2", Name = player2Name };

        _mockService
            .FindPlayerByNameAsync(player1Name, Arg.Any<CancellationToken>())
            .Returns(player1);
        _mockService
            .FindPlayerByNameAsync(player2Name, Arg.Any<CancellationToken>())
            .Returns(player2);

        // Act
        var result1 = await _controller.FindPlayerByName(player1Name);
        var result2 = await _controller.FindPlayerByName(player2Name);

        // Assert
        var okResult1 = result1.Result as OkObjectResult;
        var jsonApiDoc1 = okResult1!.Value as JsonApiDocument<PlayerNameIdDTO>;
        jsonApiDoc1!.Data!.Id.Should().Be("uid-1");

        var okResult2 = result2.Result as OkObjectResult;
        var jsonApiDoc2 = okResult2!.Value as JsonApiDocument<PlayerNameIdDTO>;
        jsonApiDoc2!.Data!.Id.Should().Be("uid-2");
    }

    [Fact]
    public async Task FindPlayerByName_InvalidOperationException_ReturnsInternalServerError()
    {
        // Arrange
        var playerName = "TestPlayer";
        var exception = new InvalidOperationException("Unexpected error");

        _mockService
            .When(x => x.FindPlayerByNameAsync(playerName, Arg.Any<CancellationToken>()))
            .Do(x => throw exception);

        // Act
        var result = await _controller.FindPlayerByName(playerName);

        // Assert
        var statusResult = result.Result as ObjectResult;
        statusResult!.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task FindPlayerByName_TimeoutException_ReturnsInternalServerError()
    {
        // Arrange
        var playerName = "TestPlayer";
        var exception = new TimeoutException("Request timeout");

        _mockService
            .When(x => x.FindPlayerByNameAsync(playerName, Arg.Any<CancellationToken>()))
            .Do(x => throw exception);

        // Act
        var result = await _controller.FindPlayerByName(playerName);

        // Assert
        var statusResult = result.Result as ObjectResult;
        statusResult!.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task FindPlayerByName_OperationCancelledException_ReturnsInternalServerError()
    {
        // Arrange
        var playerName = "TestPlayer";
        var exception = new OperationCanceledException("Request was cancelled");

        _mockService
            .When(x => x.FindPlayerByNameAsync(playerName, Arg.Any<CancellationToken>()))
            .Do(x => throw exception);

        // Act
        var result = await _controller.FindPlayerByName(playerName);

        // Assert
        var statusResult = result.Result as ObjectResult;
        statusResult!.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task FindPlayerByName_ReturnsPlayerNameIdDTO_WithCorrectProperties()
    {
        // Arrange
        var playerName = "CorrectName";
        var playerUid = "correct-uid-123";
        var expectedPlayer = new PlayerNameIdDTO
        {
            Id = playerUid,
            Name = playerName
        };

        _mockService
            .FindPlayerByNameAsync(playerName, Arg.Any<CancellationToken>())
            .Returns(expectedPlayer);

        // Act
        var result = await _controller.FindPlayerByName(playerName);

        // Assert
        var okResult = result.Result as OkObjectResult;
        var jsonApiDoc = okResult!.Value as JsonApiDocument<PlayerNameIdDTO>;

        jsonApiDoc.Should().NotBeNull();
        jsonApiDoc!.Data.Should().NotBeNull();
        jsonApiDoc.Data!.Id.Should().NotBeNull();
        jsonApiDoc.Data.Name.Should().NotBeNull();
        jsonApiDoc.Data.Id.Should().Be(playerUid);
        jsonApiDoc.Data.Name.Should().Be(playerName);
    }
}
