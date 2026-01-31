using FluentAssertions;
using Granite.Common.Services;
using Granite.Server.Configuration;
using Granite.Server.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Granite.Integration.Tests.Services;

public class VintageStoryPlayerNameResolverTests
{
    private readonly ILogger<VintageStoryPlayerNameResolver> _mockLogger;

    public VintageStoryPlayerNameResolverTests()
    {
        _mockLogger = NullLogger<VintageStoryPlayerNameResolver>.Instance;
    }

    private VintageStoryPlayerNameResolver CreateResolver(HttpClient httpClient, string? authServerUrl = null)
    {
        var options = new GraniteServerOptions 
        { 
            AuthServerUrl = authServerUrl
        };
        var mockOptions = Options.Create(options);
        return new VintageStoryPlayerNameResolver(_mockLogger, httpClient, mockOptions);
    }

    [Fact]
    public async Task ResolvePlayerNameAsync_ValidPlayerName_ReturnsPlayerUid()
    {
        // Arrange
        using var handler = new MockHttpMessageHandler();
        handler.SetResponse(
            "{\"uid\":\"player-uid-12345\"}",
            System.Net.HttpStatusCode.OK
        );
        var httpClient = new HttpClient(handler);
        var resolver = CreateResolver(httpClient, "https://auth.vintagestory.at");

        // Act
        var result = await resolver.ResolvePlayerNameAsync("TestPlayer");

        // Assert
        result.Should().NotBeNull();
        result.Should().Be("player-uid-12345");
    }

    [Fact]
    public async Task ResolvePlayerNameAsync_PlayerNotFound_ReturnsNull()
    {
        // Arrange
        using var handler = new MockHttpMessageHandler();
        handler.SetResponse(
            "{\"error\": \"Player not found\"}",
            System.Net.HttpStatusCode.NotFound
        );
        var httpClient = new HttpClient(handler);
        var resolver = CreateResolver(httpClient);

        // Act
        var result = await resolver.ResolvePlayerNameAsync("NonExistentPlayer");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolvePlayerNameAsync_EmptyPlayerName_ReturnsNull()
    {
        // Arrange
        using var handler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var resolver = CreateResolver(httpClient);

        // Act
        var result = await resolver.ResolvePlayerNameAsync(string.Empty);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolvePlayerNameAsync_NullPlayerName_ReturnsNull()
    {
        // Arrange
        using var handler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var resolver = CreateResolver(httpClient);

        // Act
        var result = await resolver.ResolvePlayerNameAsync(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolvePlayerNameAsync_NetworkError_ReturnsNull()
    {
        // Arrange
        using var handler = new MockHttpMessageHandler();
        handler.SetResponse(
            null,
            System.Net.HttpStatusCode.ServiceUnavailable
        );
        var httpClient = new HttpClient(handler);
        var resolver = CreateResolver(httpClient);

        // Act
        var result = await resolver.ResolvePlayerNameAsync("TestPlayer");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolvePlayerNameAsync_WithPlayerId_PropertyAlternative_ReturnsUid()
    {
        // Arrange - Response has "playerId" instead of "uid"
        using var handler = new MockHttpMessageHandler();
        handler.SetResponse(
            "{\"playerId\":\"alt-uid-67890\"}",
            System.Net.HttpStatusCode.OK
        );
        var httpClient = new HttpClient(handler);
        var resolver = CreateResolver(httpClient);

        // Act
        var result = await resolver.ResolvePlayerNameAsync("TestPlayer");

        // Assert
        result.Should().Be("alt-uid-67890");
    }

    [Fact]
    public async Task ResolvePlayerNameAsync_WithIdProperty_ReturnUid()
    {
        // Arrange - Response has "id" instead of "uid"
        using var handler = new MockHttpMessageHandler();
        handler.SetResponse(
            "{\"id\":\"id-11111\"}",
            System.Net.HttpStatusCode.OK
        );
        var httpClient = new HttpClient(handler);
        var resolver = CreateResolver(httpClient);

        // Act
        var result = await resolver.ResolvePlayerNameAsync("TestPlayer");

        // Assert
        result.Should().Be("id-11111");
    }

    [Fact]
    public async Task ResolvePlayerUidAsync_ValidPlayerUid_ReturnsPlayerName()
    {
        // Arrange
        using var handler = new MockHttpMessageHandler();
        handler.SetResponse(
            "{\"name\":\"TestPlayer\"}",
            System.Net.HttpStatusCode.OK
        );
        var httpClient = new HttpClient(handler);
        var resolver = CreateResolver(httpClient);

        // Act
        var result = await resolver.ResolvePlayerUidAsync("player-uid-12345");

        // Assert
        result.Should().NotBeNull();
        result.Should().Be("TestPlayer");
    }

    [Fact]
    public async Task ResolvePlayerUidAsync_UidNotFound_ReturnsNull()
    {
        // Arrange
        using var handler = new MockHttpMessageHandler();
        handler.SetResponse(
            "{\"error\": \"Player not found\"}",
            System.Net.HttpStatusCode.NotFound
        );
        var httpClient = new HttpClient(handler);
        var resolver = CreateResolver(httpClient);

        // Act
        var result = await resolver.ResolvePlayerUidAsync("nonexistent-uid");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolvePlayerUidAsync_EmptyUid_ReturnsNull()
    {
        // Arrange
        using var handler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var resolver = CreateResolver(httpClient);

        // Act
        var result = await resolver.ResolvePlayerUidAsync(string.Empty);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolvePlayerUidAsync_WithPlayerNameProperty_ReturnsName()
    {
        // Arrange - Response has "playerName" instead of "name"
        using var handler = new MockHttpMessageHandler();
        handler.SetResponse(
            "{\"playerName\":\"AlternativeName\"}",
            System.Net.HttpStatusCode.OK
        );
        var httpClient = new HttpClient(handler);
        var resolver = CreateResolver(httpClient);

        // Act
        var result = await resolver.ResolvePlayerUidAsync("player-uid");

        // Assert
        result.Should().Be("AlternativeName");
    }

    [Fact]
    public async Task ResolvePlayerUidAsync_WithUsernameProperty_ReturnsName()
    {
        // Arrange - Response has "username" instead of "name"
        using var handler = new MockHttpMessageHandler();
        handler.SetResponse(
            "{\"username\":\"UsernameValue\"}",
            System.Net.HttpStatusCode.OK
        );
        var httpClient = new HttpClient(handler);
        var resolver = CreateResolver(httpClient);

        // Act
        var result = await resolver.ResolvePlayerUidAsync("player-uid");

        // Assert
        result.Should().Be("UsernameValue");
    }

    [Fact]
    public async Task ResolvePlayerNameAsync_DefaultAuthServerUrl_UsesExpectedUrl()
    {
        // Arrange
        using var handler = new MockHttpMessageHandler();
        handler.SetResponse("{\"uid\":\"test-uid\"}", System.Net.HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);
        var resolver = CreateResolver(httpClient, null);

        // Act
        var result = await resolver.ResolvePlayerNameAsync("TestPlayer");

        // Assert
        result.Should().Be("test-uid");
    }

    [Fact]
    public async Task ResolvePlayerNameAsync_CustomAuthServerUrl_UsesCustomUrl()
    {
        // Arrange
        using var handler = new MockHttpMessageHandler();
        handler.SetResponse("{\"uid\":\"custom-uid\"}", System.Net.HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);
        var resolver = CreateResolver(httpClient, "https://custom-auth.example.com");

        // Act
        var result = await resolver.ResolvePlayerNameAsync("TestPlayer");

        // Assert
        result.Should().Be("custom-uid");
    }

    [Fact]
    public async Task ResolvePlayerNameAsync_InvalidJsonResponse_ReturnsNull()
    {
        // Arrange
        using var handler = new MockHttpMessageHandler();
        handler.SetResponse("invalid json {{{", System.Net.HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);
        var resolver = CreateResolver(httpClient);

        // Act
        var result = await resolver.ResolvePlayerNameAsync("TestPlayer");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolvePlayerNameAsync_EmptyResponse_ReturnsNull()
    {
        // Arrange - Test with empty response body
        using var handler = new MockHttpMessageHandler();
        handler.SetResponse("", System.Net.HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);
        var resolver = CreateResolver(httpClient);

        // Act
        var result = await resolver.ResolvePlayerNameAsync("TestPlayer");

        // Assert
        result.Should().BeNull();
    }
}

/// <summary>
/// Mock HTTP message handler for testing
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private System.Net.HttpStatusCode _statusCode = System.Net.HttpStatusCode.OK;
    private string? _responseContent;

    public Uri? LastRequestUri { get; private set; }

    public void SetResponse(string? content, System.Net.HttpStatusCode statusCode)
    {
        _responseContent = content;
        _statusCode = statusCode;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        LastRequestUri = request.RequestUri;

        var response = new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseContent ?? string.Empty)
        };

        return Task.FromResult(response);
    }
}

/// <summary>
/// Mock HTTP message handler that delays response for timeout testing
/// </summary>
public class MockDelayedHttpMessageHandler : HttpMessageHandler
{
    private readonly TimeSpan _delay;

    public MockDelayedHttpMessageHandler(TimeSpan delay)
    {
        _delay = delay;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        await Task.Delay(_delay, cancellationToken);
        return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
    }
}
