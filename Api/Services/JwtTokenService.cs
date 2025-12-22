using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using GraniteServer.Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace GraniteServer.Api.Services;

/// <summary>
/// Service for generating and validating JWT bearer tokens.
/// </summary>
public class JwtTokenService
{
    private readonly GraniteServerConfig _config;
    private readonly SecurityKey _securityKey;
    private readonly SigningCredentials _signingCredentials;

    public JwtTokenService(GraniteServerConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));

        if (string.IsNullOrWhiteSpace(_config.JwtSecret))
            throw new InvalidOperationException("JwtSecret is not configured.");

        _securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.JwtSecret));
        _signingCredentials = new SigningCredentials(
            _securityKey,
            SecurityAlgorithms.HmacSha256Signature
        );
    }

    /// <summary>
    /// Generates a JWT token for the given username with specified roles.
    /// </summary>
    public TokenDTO GenerateToken(string username, params string[] roles)
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

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_config.JwtExpiryMinutes),
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
            ExpiresIn = _config.JwtExpiryMinutes * 60,
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
            return jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
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
