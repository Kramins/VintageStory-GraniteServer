using Granite.Common.Services;
using Granite.Server.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Granite.Server.Services;

/// <summary>
/// Implementation of IPlayerNameResolver that queries the Vintage Story authentication server.
/// The auth server URL can be configured via environment variable GS_AUTH_SERVER_URL or in configuration.
/// </summary>
public class VintageStoryPlayerNameResolver : IPlayerNameResolver
{
    private readonly ILogger<VintageStoryPlayerNameResolver> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _authServerUrl;

    public VintageStoryPlayerNameResolver(
        ILogger<VintageStoryPlayerNameResolver> logger,
        HttpClient httpClient,
        IOptions<GraniteServerOptions> options
    )
    {
        _logger = logger;
        _httpClient = httpClient;
        _authServerUrl = options.Value.AuthServerUrl ?? "https://auth.vintagestory.at";
    }

    /// <summary>
    /// Resolves a player name to their unique player UID by contacting the Vintage Story auth server.
    /// Note: This should be used sparingly as it contacts an external server.
    /// </summary>
    public async Task<string?> ResolvePlayerNameAsync(string playerName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(playerName))
        {
            return null;
        }

        try
        {
            var url = $"{_authServerUrl.TrimEnd('/')}/api/v1/player/name/{Uri.EscapeDataString(playerName)}";
            _logger.LogDebug("Resolving player name '{PlayerName}' via {Url}", playerName, url);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var uid = ExtractUidFromJson(json);

            if (!string.IsNullOrEmpty(uid))
            {
                _logger.LogDebug("Resolved player name '{PlayerName}' to UID '{PlayerUid}'", playerName, uid);
                return uid;
            }

            _logger.LogWarning("Failed to resolve player name '{PlayerName}': No UID in response", playerName);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to resolve player name '{PlayerName}': HTTP error", playerName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve player name '{PlayerName}'", playerName);
            return null;
        }
    }

    /// <summary>
    /// Resolves a player UID to their current player name by contacting the Vintage Story auth server.
    /// Note: This should be used sparingly as it contacts an external server.
    /// </summary>
    public async Task<string?> ResolvePlayerUidAsync(string playerUid, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(playerUid))
        {
            return null;
        }

        try
        {
            var url = $"{_authServerUrl.TrimEnd('/')}/api/v1/player/uid/{Uri.EscapeDataString(playerUid)}";
            _logger.LogDebug("Resolving player UID '{PlayerUid}' via {Url}", playerUid, url);

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var name = ExtractNameFromJson(json);

            if (!string.IsNullOrEmpty(name))
            {
                _logger.LogDebug("Resolved player UID '{PlayerUid}' to name '{PlayerName}'", playerUid, name);
                return name;
            }

            _logger.LogWarning("Failed to resolve player UID '{PlayerUid}': No name in response", playerUid);
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to resolve player UID '{PlayerUid}': HTTP error", playerUid);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve player UID '{PlayerUid}'", playerUid);
            return null;
        }
    }

    /// <summary>
    /// Extracts the player UID from the auth server JSON response.
    /// Adjust this based on the actual response format from the auth server.
    /// </summary>
    private static string? ExtractUidFromJson(string json)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Try common JSON property names for UID
            if (root.TryGetProperty("uid", out var uidElement) ||
                root.TryGetProperty("playerId", out uidElement) ||
                root.TryGetProperty("id", out uidElement))
            {
                return uidElement.GetString();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extracts the player name from the auth server JSON response.
    /// Adjust this based on the actual response format from the auth server.
    /// </summary>
    private static string? ExtractNameFromJson(string json)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Try common JSON property names for name
            if (root.TryGetProperty("name", out var nameElement) ||
                root.TryGetProperty("playerName", out nameElement) ||
                root.TryGetProperty("username", out nameElement))
            {
                return nameElement.GetString();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
