using Granite.Web.Client.Services.Api;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Granite.Web.Tests.Services.Api;

public class ServerApiClientTests
{
    private readonly Mock<ILogger<ServerApiClient>> _mockLogger;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly MockHttpMessageHandler _mockHttpHandler;
    private readonly HttpClient _httpClient;
    private readonly ServerApiClient _apiClient;

    public ServerApiClientTests()
    {
        _mockLogger = new Mock<ILogger<ServerApiClient>>();
        _mockHttpHandler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_mockHttpHandler)
        {
            BaseAddress = new Uri("http://localhost:5000")
        };
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockHttpClientFactory.Setup(f => f.CreateClient("GraniteApi")).Returns(_httpClient);
        _apiClient = new ServerApiClient(_mockHttpClientFactory.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetServersAsync_WithValidResponse_ReturnsServerList()
    {
        // Arrange
        var document = new { data = new List<object> { new { id = "550e8400-e29b-41d4-a716-446655440000", name = "Main Server" } } };
        var json = System.Text.Json.JsonSerializer.Serialize(document);
        _mockHttpHandler.SetResponse(json, System.Net.HttpStatusCode.OK);

        // Act
        var result = await _apiClient.GetServersAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task GetServerStatusAsync_WithValidServerId_ReturnsStatus()
    {
        // Arrange
        var document = new { data = new { isOnline = true, playerCount = 5 } };
        var json = System.Text.Json.JsonSerializer.Serialize(document);
        _mockHttpHandler.SetResponse(json, System.Net.HttpStatusCode.OK);

        // Act
        var result = await _apiClient.GetServerStatusAsync("server1");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        _mockHttpHandler.VerifyRequest("/api/server1/server/status", System.Net.Http.HttpMethod.Get);
    }

    [Fact]
    public async Task RestartServerAsync_WithValidServerId_MakesCorrectRequest()
    {
        // Arrange
        var document = new { data = (object?)null };
        var json = System.Text.Json.JsonSerializer.Serialize(document);
        _mockHttpHandler.SetResponse(json, System.Net.HttpStatusCode.OK);

        // Act
        var result = await _apiClient.RestartServerAsync("server1");

        // Assert
        Assert.NotNull(result);
        _mockHttpHandler.VerifyRequest("/api/servers/server1/restart", System.Net.Http.HttpMethod.Post);
    }

    [Fact]
    public async Task StopServerAsync_WithValidServerId_MakesCorrectRequest()
    {
        // Arrange
        var document = new { data = (object?)null };
        var json = System.Text.Json.JsonSerializer.Serialize(document);
        _mockHttpHandler.SetResponse(json, System.Net.HttpStatusCode.OK);

        // Act
        var result = await _apiClient.StopServerAsync("server1");

        // Assert
        Assert.NotNull(result);
        _mockHttpHandler.VerifyRequest("/api/servers/server1/stop", System.Net.Http.HttpMethod.Post);
    }

    [Fact]
    public async Task GetHealthAsync_WithServerError_ThrowsApiException()
    {
        // Arrange
        _mockHttpHandler.SetResponse("Internal Server Error", System.Net.HttpStatusCode.InternalServerError);

        // Act & Assert
        await Assert.ThrowsAsync<ApiException>(() => _apiClient.GetHealthAsync("server1"));
    }
}
