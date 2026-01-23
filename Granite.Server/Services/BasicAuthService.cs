using System;
using Granite.Server.Configuration;
using Microsoft.Extensions.Options;

namespace Granite.Server.Services;

/// <summary>
/// Service for validating basic authentication credentials.
/// Uses the username and password configured in GraniteServerOptions.
/// </summary>
public class BasicAuthService
{
    private readonly ILogger<BasicAuthService> _logger;
    private readonly GraniteServerOptions _options;

    public BasicAuthService(ILogger<BasicAuthService> logger, IOptions<GraniteServerOptions> options)
    {
        _logger = logger;
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Validates the provided credentials against the configured username and password.
    /// </summary>
    /// <returns>True if credentials are valid, false otherwise.</returns>
    public bool ValidateCredentials(string? username, string? password)
    {
        // Check if credentials are provided
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return false;

        // Check if config has credentials set
        if (
            string.IsNullOrWhiteSpace(_options.Username)
            || string.IsNullOrWhiteSpace(_options.Password)
        )
            return false;

        // Use constant-time comparison to prevent timing attacks
        bool usernameMatch = username == _options.Username;
        bool passwordMatch = password == _options.Password;

        return usernameMatch && passwordMatch;
    }
}
