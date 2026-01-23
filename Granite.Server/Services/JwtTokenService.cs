using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Granite.Common.Dto;
using Granite.Server.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Granite.Server.Services;

/// <summary>
/// Service for generating and validating JWT bearer tokens.
/// </summary>
public class JwtTokenService
{
    private readonly ILogger<JwtTokenService> _logger;
    private readonly GraniteServerOptions _options;
    private readonly SecurityKey _securityKey;
    private readonly SigningCredentials _signingCredentials;

    public JwtTokenService(ILogger<JwtTokenService> logger, IOptions<GraniteServerOptions> options)
    {
        _logger = logger;
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        if (string.IsNullOrWhiteSpace(_options.JwtSecret))
            throw new InvalidOperationException("JwtSecret is not configured.");

        _securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.JwtSecret));
        _signingCredentials = new SigningCredentials(
            _securityKey,
            SecurityAlgorithms.HmacSha256Signature
        );
    }

    /// <summary>
    /// Generates a JWT token for a user with specified roles.
    /// </summary>
    public TokenDTO GenerateUserToken(string username, params string[] roles)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be empty.", nameof(username));

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, username),
            new Claim(ClaimTypes.Name, username),
        };

        foreach (var role in roles ?? Array.Empty<string>())
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        return CreateToken(claims);
    }

    /// <summary>
    /// Generates a JWT token for a mod client with server identity.
    /// </summary>
    public TokenDTO GenerateModToken(Guid serverId, string serverName)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "ModClient"),
            new Claim(ClaimTypes.Name, serverName),
            new Claim(ClaimTypes.Role, "Mod"),
            new Claim("ServerId", serverId.ToString()),
        };

        return CreateToken(claims);
    }

    private TokenDTO CreateToken(List<Claim> claims)
    {
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_options.JwtExpiryMinutes),
            NotBefore = DateTime.UtcNow.AddMinutes(-5),
            SigningCredentials = _signingCredentials,
            Issuer = "GraniteServer",
            Audience = "GraniteServerClient",
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return new TokenDTO
        {
            AccessToken = tokenHandler.WriteToken(token),
            TokenType = "bearer",
            ExpiresIn = _options.JwtExpiryMinutes * 60,
            RefreshToken = null,
            Scope = null,
        };
    }

    /// <summary>
    /// Validates a JWT token and extracts claims if valid.
    /// </summary>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(
                token,
                new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = _securityKey,
                    ValidateIssuer = true,
                    ValidIssuer = "GraniteServer",
                    ValidateAudience = true,
                    ValidAudience = "GraniteServerClient",
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                },
                out SecurityToken validatedToken
            );

            return principal;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extracts the username from a token without validation.
    /// </summary>
    public string? GetUsernameFromToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            if (!tokenHandler.CanReadToken(token))
                return null;

            var jwtToken = tokenHandler.ReadJwtToken(token);
            var nameClaim = jwtToken.Claims.FirstOrDefault(c =>
                c.Type == ClaimTypes.Name
                || c.Type == JwtRegisteredClaimNames.UniqueName
                || c.Type == "name"
            );
            return nameClaim?.Value;
        }
        catch
        {
            return null;
        }
    }

    public ICollection<SecurityKey> GetSigningKeys()
    {
        return new List<SecurityKey> { _securityKey };
    }
}
