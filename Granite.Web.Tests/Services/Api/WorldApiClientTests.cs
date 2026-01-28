using Granite.Web.Client.Services.Api;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Granite.Web.Tests.Services.Api;

public class WorldApiClientTests
{
    private readonly Mock<ILogger<WorldApiClient>> _mockLogger;
    private readonly MockHttpMessageHandler _mockHttpHandler;
    private readonly HttpClient _httpClient;
    private readonly WorldApiClient _apiClient;

    public WorldApiClientTests()
    {
        _mockLogger = new Mock<ILogger<WorldApiClient>>();
        _mockHttpHandler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_mockHttpHandler)
        {
            BaseAddress = new Uri("http://localhost:5000")
        };
        _apiClient = new WorldApiClient(_httpClient, _mockLogger.Object);
    }

    [Fact]
    public async Task GetWorldInfoAsync_WithValidServerId_ReturnsWorldInfo()
    {
        // Arrange
        var document = new { data = new { seed = "12345", size = 1024 } };
        var json = System.Text.Json.JsonSerializer.Serialize(document);
        _mockHttpHandler.SetResponse(json, System.Net.HttpStatusCode.OK);

        // Act
        var result = await _apiClient.GetWorldInfoAsync("server1");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        _mockHttpHandler.VerifyRequest("/api/world/info", System.Net.Http.HttpMethod.Get);
    }

    [Fact]
    public async Task GetWorldSeedAsync_WithValidServerId_ReturnsSeed()
    {
        // Arrange
        var document = new { data = "12345" };
        var json = System.Text.Json.JsonSerializer.Serialize(document);
        _mockHttpHandler.SetResponse(json, System.Net.HttpStatusCode.OK);

        // Act
        var result = await _apiClient.GetWorldSeedAsync("server1");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        _mockHttpHandler.VerifyRequest("/api/world/seed", System.Net.Http.HttpMethod.Get);
    }

    [Fact]
    public async Task SaveWorldAsync_WithValidServerId_MakesCorrectRequest()
    {
        // Arrange
        var document = new { data = (object?)null };
        var json = System.Text.Json.JsonSerializer.Serialize(document);
        _mockHttpHandler.SetResponse(json, System.Net.HttpStatusCode.OK);

        // Act
        var result = await _apiClient.SaveWorldAsync("server1");

        // Assert
        Assert.NotNull(result);
        _mockHttpHandler.VerifyRequest("/api/world/save", System.Net.Http.HttpMethod.Post);
    }

    [Fact]
    public async Task GetWorldMapAsync_WithValidServerId_ReturnsMap()
    {
        // Arrange
        var document = new { data = new { width = 1024, height = 1024 } };
        var json = System.Text.Json.JsonSerializer.Serialize(document);
        _mockHttpHandler.SetResponse(json, System.Net.HttpStatusCode.OK);

        // Act
        var result = await _apiClient.GetWorldMapAsync("server1");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        _mockHttpHandler.VerifyRequest("/api/world/map", System.Net.Http.HttpMethod.Get);
    }

    [Fact]
    public async Task GetCollectiblesAsync_WithValidServerId_ReturnsCollectibles()
    {
        // Arrange
        var document = new { data = new List<object> { new { id = "col1", type = "ore" } } };
        var json = System.Text.Json.JsonSerializer.Serialize(document);
        _mockHttpHandler.SetResponse(json, System.Net.HttpStatusCode.OK);

        // Act
        var result = await _apiClient.GetCollectiblesAsync("server1");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        _mockHttpHandler.VerifyRequest("/api/world/collectibles", System.Net.Http.HttpMethod.Get);
    }
}
