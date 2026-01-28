using Granite.Web.Client.Services.Api;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Granite.Web.Tests.Services.Api;

public class ModsApiClientTests
{
    private readonly Mock<ILogger<ModsApiClient>> _mockLogger;
    private readonly MockHttpMessageHandler _mockHttpHandler;
    private readonly HttpClient _httpClient;
    private readonly ModsApiClient _apiClient;

    public ModsApiClientTests()
    {
        _mockLogger = new Mock<ILogger<ModsApiClient>>();
        _mockHttpHandler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_mockHttpHandler)
        {
            BaseAddress = new Uri("http://localhost:5000")
        };
        _apiClient = new ModsApiClient(_httpClient, _mockLogger.Object);
    }

    [Fact]
    public async Task GetModsAsync_WithValidResponse_ReturnsModList()
    {
        // Arrange
        var document = new { data = new List<object> { new { id = "mod1", name = "TestMod" } } };
        var json = System.Text.Json.JsonSerializer.Serialize(document);
        _mockHttpHandler.SetResponse(json, System.Net.HttpStatusCode.OK);

        // Act
        var result = await _apiClient.GetModsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task GetModAsync_WithValidModId_ReturnsMod()
    {
        // Arrange
        var document = new { data = new { id = "mod1", name = "TestMod" } };
        var json = System.Text.Json.JsonSerializer.Serialize(document);
        _mockHttpHandler.SetResponse(json, System.Net.HttpStatusCode.OK);

        // Act
        var result = await _apiClient.GetModAsync("mod1");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        _mockHttpHandler.VerifyRequest("/api/mods/mod1", System.Net.Http.HttpMethod.Get);
    }

    [Fact]
    public async Task InstallModAsync_WithValidModId_MakesCorrectRequest()
    {
        // Arrange
        var document = new { data = (object?)null };
        var json = System.Text.Json.JsonSerializer.Serialize(document);
        _mockHttpHandler.SetResponse(json, System.Net.HttpStatusCode.OK);

        // Act
        var result = await _apiClient.InstallModAsync("mod1");

        // Assert
        Assert.NotNull(result);
        _mockHttpHandler.VerifyRequest("/api/mods/mod1/install", System.Net.Http.HttpMethod.Post);
    }

    [Fact]
    public async Task UninstallModAsync_WithValidModId_MakesCorrectRequest()
    {
        // Arrange
        var document = new { data = (object?)null };
        var json = System.Text.Json.JsonSerializer.Serialize(document);
        _mockHttpHandler.SetResponse(json, System.Net.HttpStatusCode.OK);

        // Act
        var result = await _apiClient.UninstallModAsync("mod1");

        // Assert
        Assert.NotNull(result);
        _mockHttpHandler.VerifyRequest("/api/mods/mod1", System.Net.Http.HttpMethod.Delete);
    }

    [Fact]
    public async Task GetModStatusAsync_WithValidModId_ReturnsStatus()
    {
        // Arrange
        var document = new { data = new { isInstalled = true, version = "1.0.0" } };
        var json = System.Text.Json.JsonSerializer.Serialize(document);
        _mockHttpHandler.SetResponse(json, System.Net.HttpStatusCode.OK);

        // Act
        var result = await _apiClient.GetModStatusAsync("mod1");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
    }
}
