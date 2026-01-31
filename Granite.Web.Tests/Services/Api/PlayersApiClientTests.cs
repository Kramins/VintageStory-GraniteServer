using Granite.Web.Client.Services.Api;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Granite.Web.Tests.Services.Api;

public class PlayersApiClientTests
{
    private readonly Mock<ILogger<ServerPlayersApiClient>> _mockLogger;
    private readonly MockHttpMessageHandler _mockHttpHandler;
    private readonly HttpClient _httpClient;
    private readonly ServerPlayersApiClient _apiClient;

    public PlayersApiClientTests()
    {
        _mockLogger = new Mock<ILogger<ServerPlayersApiClient>>();
        _mockHttpHandler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_mockHttpHandler)
        {
            BaseAddress = new Uri("http://localhost:5000")
        };
        _apiClient = new PlayersApiClient(_httpClient, _mockLogger.Object);
    }

    [Fact]
    public async Task GetPlayersAsync_WithValidResponse_ReturnsPlayerList()
    {
        // Arrange
        var document = new { data = new List<object>() };
        var json = System.Text.Json.JsonSerializer.Serialize(document);
        _mockHttpHandler.SetResponse(json, System.Net.HttpStatusCode.OK);

        // Act
        var result = await _apiClient.GetPlayersAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task GetPlayerAsync_WithValidPlayerId_ReturnsPlayer()
    {
        // Arrange
        var document = new { data = new { playerUID = "uid1", name = "Player1" } };
        var json = System.Text.Json.JsonSerializer.Serialize(document);
        _mockHttpHandler.SetResponse(json, System.Net.HttpStatusCode.OK);

        // Act
        var result = await _apiClient.GetPlayerAsync("uid1");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        _mockHttpHandler.VerifyRequest("/api/players/uid1", System.Net.Http.HttpMethod.Get);
    }

    [Fact]
    public async Task GetPlayerAsync_WithNotFoundResponse_ThrowsApiException()
    {
        // Arrange
        _mockHttpHandler.SetResponse("", System.Net.HttpStatusCode.NotFound);

        // Act & Assert
        await Assert.ThrowsAsync<ApiException>(() => _apiClient.GetPlayerAsync("nonexistent"));
    }

    [Fact]
    public async Task KickPlayerAsync_WithValidPlayerId_MakesCorrectRequest()
    {
        // Arrange
        _mockHttpHandler.SetResponse("{}", System.Net.HttpStatusCode.OK);

        // Act
        await _apiClient.KickPlayerAsync("uid1", "Reason");

        // Assert
        _mockHttpHandler.VerifyRequest("/api/players/uid1/kick", System.Net.Http.HttpMethod.Post);
    }

    [Fact]
    public async Task BanPlayerAsync_WithValidPlayerId_MakesCorrectRequest()
    {
        // Arrange
        _mockHttpHandler.SetResponse("{}", System.Net.HttpStatusCode.OK);

        // Act
        await _apiClient.BanPlayerAsync("uid1", "Reason");

        // Assert
        _mockHttpHandler.VerifyRequest("/api/players/uid1/ban", System.Net.Http.HttpMethod.Post);
    }

    [Fact]
    public async Task WhitelistPlayerAsync_WithValidPlayerId_MakesCorrectRequest()
    {
        // Arrange
        _mockHttpHandler.SetResponse("{}", System.Net.HttpStatusCode.OK);

        // Act
        await _apiClient.WhitelistPlayerAsync("uid1");

        // Assert
        _mockHttpHandler.VerifyRequest("/api/players/uid1/whitelist", System.Net.Http.HttpMethod.Post);
    }

    [Fact]
    public async Task RemoveFromWhitelistAsync_WithValidPlayerId_MakesCorrectRequest()
    {
        // Arrange
        _mockHttpHandler.SetResponse("{}", System.Net.HttpStatusCode.OK);

        // Act
        await _apiClient.RemoveFromWhitelistAsync("uid1");

        // Assert
        _mockHttpHandler.VerifyRequest("/api/players/uid1/whitelist", System.Net.Http.HttpMethod.Delete);
    }
}
