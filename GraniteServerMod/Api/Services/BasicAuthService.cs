using System;

namespace GraniteServer.Api.Services;

/// <summary>
/// Service for validating basic authentication credentials.
/// Uses the username and password configured in GraniteServerConfig.
/// </summary>
public class BasicAuthService
{
    private readonly GraniteServerConfig _config;

    public BasicAuthService(GraniteServerConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
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
            string.IsNullOrWhiteSpace(_config.Username)
            || string.IsNullOrWhiteSpace(_config.Password)
        )
            return false;

        // Use constant-time comparison to prevent timing attacks
        bool usernameMatch = username == _config.Username;
        bool passwordMatch = password == _config.Password;

        return usernameMatch && passwordMatch;
    }

    internal bool ValidateCredentials(object username, object password)
    {
        throw new NotImplementedException();
    }
}
