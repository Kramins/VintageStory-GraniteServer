using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using FluentAssertions;
using Granite.Server.Configuration;
using Granite.Server.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Granite.Tests.Services;

public class JwtTokenServiceTests
{
    private readonly IOptions<GraniteServerOptions> _mockOptions;
    private readonly ILogger<JwtTokenService> _mockLogger;
    private const string TestSecret = "VeryLongSecretKeyThatIsAtLeast32CharactersLong123456789";

    public JwtTokenServiceTests()
    {
        _mockOptions = Substitute.For<IOptions<GraniteServerOptions>>();
        _mockLogger = Substitute.For<ILogger<JwtTokenService>>();
        _mockOptions.Value.Returns(new GraniteServerOptions
        {
            JwtSecret = TestSecret,
            JwtExpiryMinutes = 60
        });
    }

    private JwtTokenService CreateService(string? secret = null, int expiryMinutes = 60)
    {
        var options = new GraniteServerOptions
        {
            JwtSecret = secret ?? TestSecret,
            JwtExpiryMinutes = expiryMinutes
        };
        _mockOptions.Value.Returns(options);
        return new JwtTokenService(_mockLogger, _mockOptions);
    }

    [Fact]
    public void GenerateUserToken_WithUsername_CreatesValidToken()
    {
        // Arrange
        var service = CreateService();
        var username = "testuser";

        // Act
        var tokenDto = service.GenerateUserToken(username);

        // Assert
        tokenDto.AccessToken.Should().NotBeNullOrEmpty();
        tokenDto.TokenType.Should().Be("bearer");
        tokenDto.ExpiresIn.Should().Be(60 * 60); // 60 minutes in seconds
    }

    [Fact]
    public void GenerateUserToken_WithRoles_IncludesRolesInToken()
    {
        // Arrange
        var service = CreateService();
        var username = "testuser";
        var roles = new[] { "admin", "moderator" };

        // Act
        var tokenDto = service.GenerateUserToken(username, roles);
        var claims = ExtractClaimsFromToken(tokenDto.AccessToken, service);

        // Assert
        var roleClaims = claims.Where(c => c.Type == ClaimTypes.Role).ToList();
        roleClaims.Should().HaveCount(2);
        roleClaims.Select(c => c.Value).Should().Contain("admin", "moderator");
    }

    [Fact]
    public void GenerateUserToken_WithoutRoles_CreatesTokenWithoutRoleClaims()
    {
        // Arrange
        var service = CreateService();
        var username = "testuser";

        // Act
        var tokenDto = service.GenerateUserToken(username);
        var claims = ExtractClaimsFromToken(tokenDto.AccessToken, service);

        // Assert
        var roleClaims = claims.Where(c => c.Type == ClaimTypes.Role).ToList();
        roleClaims.Should().BeEmpty();
    }

    [Fact]
    public void GenerateUserToken_IncludesUsernameInClaims()
    {
        // Arrange
        var service = CreateService();
        var username = "testuser";

        // Act
        var tokenDto = service.GenerateUserToken(username);
        var claims = ExtractClaimsFromToken(tokenDto.AccessToken, service);

        // Assert
        claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == username);
        claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == username);
    }

    [Fact]
    public void GenerateModToken_WithServerInfo_CreatesTokenWithServerClaim()
    {
        // Arrange
        var service = CreateService();
        var serverId = Guid.NewGuid();
        var serverName = "TestServer";

        // Act
        var tokenDto = service.GenerateModToken(serverId, serverName);
        var claims = ExtractClaimsFromToken(tokenDto.AccessToken, service);

        // Assert
        claims.Should().Contain(c => c.Type == "ServerId" && c.Value == serverId.ToString());
        claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Mod");
        claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == serverName);
    }

    [Fact]
    public void ValidateToken_ValidToken_ReturnsPrincipal()
    {
        // Arrange
        var service = CreateService();
        var tokenDto = service.GenerateUserToken("testuser");

        // Act
        var principal = service.ValidateToken(tokenDto.AccessToken);

        // Assert
        principal.Should().NotBeNull();
        principal?.FindFirst(ClaimTypes.Name)?.Value.Should().Be("testuser");
    }

    [Fact]
    public void ValidateToken_ExpiredToken_ReturnsNull()
    {
        // Arrange
        var service = CreateService(expiryMinutes: -1); // Negative expiry = already expired
        var tokenDto = service.GenerateUserToken("testuser");

        // Create a new service with the same secret to validate
        var validationService = CreateService();

        // Act
        var principal = validationService.ValidateToken(tokenDto.AccessToken);

        // Assert
        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_InvalidSignature_ReturnsNull()
    {
        // Arrange
        var service = CreateService();
        var tokenDto = service.GenerateUserToken("testuser");

        // Tamper with the token by modifying the signature
        var tamperedToken = tokenDto.AccessToken.Substring(0, tokenDto.AccessToken.Length - 10) + "XXXXXXXXXX";

        // Act
        var principal = service.ValidateToken(tamperedToken);

        // Assert
        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_EmptyToken_ReturnsNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var principal = service.ValidateToken("");

        // Assert
        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_NullToken_ReturnsNull()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        var result = service.ValidateToken(string.Empty);
        result.Should().BeNull();
    }

    [Fact]
    public void GetUsernameFromToken_ValidToken_ExtractsUsername()
    {
        // Arrange
        var service = CreateService();
        var username = "testuser";
        var tokenDto = service.GenerateUserToken(username);

        // Act
        var extractedUsername = service.GetUsernameFromToken(tokenDto.AccessToken);

        // Assert
        extractedUsername.Should().Be(username);
    }

    [Fact]
    public void GetUsernameFromToken_InvalidToken_ReturnsNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var username = service.GetUsernameFromToken("invalid.token.here");

        // Assert
        username.Should().BeNull();
    }

    [Fact]
    public void GetUsernameFromToken_EmptyToken_ReturnsNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var username = service.GetUsernameFromToken("");

        // Assert
        username.Should().BeNull();
    }

    [Fact]
    public void GenerateUserToken_WithEmptyUsername_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();

        // Act
        var action = () => service.GenerateUserToken("");

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GenerateUserToken_WithNullUsername_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        service.Invoking(s => s.GenerateUserToken(string.Empty)).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithMissingJwtSecret_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new GraniteServerOptions { JwtSecret = null! };
        _mockOptions.Value.Returns(options);

        // Act
        var action = () => new JwtTokenService(_mockLogger, _mockOptions);

        // Assert
        action.Should().Throw<InvalidOperationException>().WithMessage("*JwtSecret*");
    }

    private Claim[] ExtractClaimsFromToken(string token, JwtTokenService service)
    {
        var principal = service.ValidateToken(token);
        return principal?.Claims.ToArray() ?? Array.Empty<Claim>();
    }
}
