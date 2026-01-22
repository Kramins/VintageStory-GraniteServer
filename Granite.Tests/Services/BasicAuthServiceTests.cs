using FluentAssertions;
using Granite.Server.Configuration;
using Granite.Server.Services;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Granite.Tests.Services;

public class BasicAuthServiceTests
{
    private readonly IOptions<GraniteServerOptions> _mockOptions;

    public BasicAuthServiceTests()
    {
        _mockOptions = Substitute.For<IOptions<GraniteServerOptions>>();
    }

    private BasicAuthService CreateService(string? username = null, string? password = null)
    {
        var options = new GraniteServerOptions { Username = username ?? string.Empty, Password = password ?? string.Empty };
        _mockOptions.Value.Returns(options);
        return new BasicAuthService(_mockOptions);
    }

    [Fact]
    public void ValidateCredentials_ValidUsernameAndPassword_ReturnsTrue()
    {
        // Arrange
        var service = CreateService("admin", "secret123");

        // Act
        var result = service.ValidateCredentials("admin", "secret123");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateCredentials_InvalidUsername_ReturnsFalse()
    {
        // Arrange
        var service = CreateService("admin", "secret123");

        // Act
        var result = service.ValidateCredentials("wronguser", "secret123");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateCredentials_InvalidPassword_ReturnsFalse()
    {
        // Arrange
        var service = CreateService("admin", "secret123");

        // Act
        var result = service.ValidateCredentials("admin", "wrongpass");

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(null, "password")]
    [InlineData("username", null)]
    [InlineData(null, null)]
    [InlineData("", "password")]
    [InlineData("username", "")]
    [InlineData("  ", "  ")]
    public void ValidateCredentials_NullOrEmptyCredentials_ReturnsFalse(string? username, string? password)
    {
        // Arrange
        var service = CreateService("admin", "secret123");

        // Act
        var result = service.ValidateCredentials(username, password);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateCredentials_UnconfiguredServerCredentials_ReturnsFalse()
    {
        // Arrange
        var service = CreateService(null, null);

        // Act
        var result = service.ValidateCredentials("admin", "secret123");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateCredentials_BothUsernameAndPasswordMustMatch_ReturnsFalse()
    {
        // Arrange
        var service = CreateService("admin", "secret123");

        // Act
        var result1 = service.ValidateCredentials("admin", "wrongpass");
        var result2 = service.ValidateCredentials("wronguser", "secret123");

        // Assert
        result1.Should().BeFalse();
        result2.Should().BeFalse();
    }
}
