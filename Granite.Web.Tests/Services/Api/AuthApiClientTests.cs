using Granite.Web.Client.Services.Api;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Granite.Web.Tests.Services.Api;

public class AuthApiClientTests
{
    private readonly Mock<ILogger<AuthApiClient>> _mockLogger;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly MockHttpMessageHandler _mockHttpHandler;
    private readonly HttpClient _httpClient;
    private readonly AuthApiClient _apiClient;

    public AuthApiClientTests()
    {
        _mockLogger = new Mock<ILogger<AuthApiClient>>();
        _mockHttpHandler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(_mockHttpHandler)
        {
            BaseAddress = new Uri("http://localhost:5000")
        };
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockHttpClientFactory.Setup(f => f.CreateClient("GraniteApi")).Returns(_httpClient);
        _apiClient = new AuthApiClient(_mockHttpClientFactory.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsToken()
    {
        // Arrange
        var document = new { data = new { accessToken = "token123", refreshToken = "refresh123" } };
        var json = System.Text.Json.JsonSerializer.Serialize(document);
        _mockHttpHandler.SetResponse(json, System.Net.HttpStatusCode.OK);

        // Act
        var result = await _apiClient.LoginAsync("user", "password");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        _mockHttpHandler.VerifyRequest("/api/auth/login", System.Net.Http.HttpMethod.Post);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidCredentials_ThrowsApiException()
    {
        // Arrange
        _mockHttpHandler.SetResponse("Unauthorized", System.Net.HttpStatusCode.Unauthorized);

        // Act & Assert
        await Assert.ThrowsAsync<ApiException>(() => _apiClient.LoginAsync("user", "wrongpassword"));
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ReturnsNewToken()
    {
        // Arrange
        var document = new { data = new { accessToken = "newtoken", refreshToken = "newrefresh" } };
        var json = System.Text.Json.JsonSerializer.Serialize(document);
        _mockHttpHandler.SetResponse(json, System.Net.HttpStatusCode.OK);

        // Act
        var result = await _apiClient.RefreshTokenAsync("refresh123");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        _mockHttpHandler.VerifyRequest("/api/auth/refresh", System.Net.Http.HttpMethod.Post);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithValidToken_ReturnsTrue()
    {
        // Arrange
        var document = new { data = true };
        var json = System.Text.Json.JsonSerializer.Serialize(document);
        _mockHttpHandler.SetResponse(json, System.Net.HttpStatusCode.OK);

        // Act
        var result = await _apiClient.ValidateTokenAsync("token123");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Data);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithInvalidToken_ReturnsFalse()
    {
        // Arrange
        _mockHttpHandler.SetResponse("Unauthorized", System.Net.HttpStatusCode.Unauthorized);

        // Act
        var result = await _apiClient.ValidateTokenAsync("invalid");

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Data);
    }

    [Fact]
    public async Task LogoutAsync_MakesCorrectRequest()
    {
        // Arrange
        var document = new { data = (object?)null };
        var json = System.Text.Json.JsonSerializer.Serialize(document);
        _mockHttpHandler.SetResponse(json, System.Net.HttpStatusCode.OK);

        // Act
        var result = await _apiClient.LogoutAsync();

        // Assert
        Assert.NotNull(result);
        _mockHttpHandler.VerifyRequest("/api/auth/logout", System.Net.Http.HttpMethod.Post);
    }

    [Fact]
    public async Task GetAuthSettingsAsync_WithValidResponse_ReturnsSettings()
    {
        // Arrange
        var document = new { data = new { requiresAuth = true, allowOAuth = true } };
        var json = System.Text.Json.JsonSerializer.Serialize(document);
        _mockHttpHandler.SetResponse(json, System.Net.HttpStatusCode.OK);

        // Act
        var result = await _apiClient.GetAuthSettingsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        _mockHttpHandler.VerifyRequest("/api/auth/settings", System.Net.Http.HttpMethod.Get);
    }
}
