using System.Text.Json.Serialization;
using Granite.Common.Services;
using Granite.Server.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Granite.Server.Services;

/// <summary>
/// Implementation of IPlayerNameResolver that queries the Vintage Story authentication server.
/// Based on reverse engineering of VintageStory's AuthServerComm class.
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
        _authServerUrl = options.Value.AuthServerUrl ?? "https://auth3.vintagestory.at";
    }

    /// <summary>
    /// Resolves a player name to their unique player UID by contacting the Vintage Story auth server.
    /// Note: This should be used sparingly as it contacts an external server.
    /// </summary>
    public async Task<string?> ResolvePlayerNameAsync(
        string playerName,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(playerName))
        {
            return null;
        }

        try
        {
            var url = $"{_authServerUrl.TrimEnd('/')}/resolveplayername";
            _logger.LogDebug("Resolving player name '{PlayerName}' via {Url}", playerName, url);

            // VintageStory uses POST with form data, not GET with path parameters
            var formData = new Dictionary<string, string> { { "playername", playerName } };
            var content = new FormUrlEncodedContent(formData);

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("Response from auth server: {Response}", json);

            var resolveResponse = System.Text.Json.JsonSerializer.Deserialize<ResolveResponse>(
                json
            );

            if (resolveResponse?.PlayerUid != null && resolveResponse.Valid == 1)
            {
                _logger.LogDebug(
                    "Resolved player name '{PlayerName}' to UID '{PlayerUid}'",
                    playerName,
                    resolveResponse.PlayerUid
                );
                return resolveResponse.PlayerUid;
            }

            _logger.LogWarning(
                "Failed to resolve player name '{PlayerName}': No UID in response or invalid",
                playerName
            );
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Failed to resolve player name '{PlayerName}': HTTP error",
                playerName
            );
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
    public async Task<string?> ResolvePlayerUidAsync(
        string playerUid,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(playerUid))
        {
            return null;
        }

        try
        {
            var url = $"{_authServerUrl.TrimEnd('/')}/resolveplayeruid";
            _logger.LogDebug("Resolving player UID '{PlayerUid}' via {Url}", playerUid, url);

            // VintageStory uses POST with form data, not GET with path parameters
            var formData = new Dictionary<string, string> { { "uid", playerUid } };
            var content = new FormUrlEncodedContent(formData);

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("Response from auth server: {Response}", json);

            var resolveResponse = System.Text.Json.JsonSerializer.Deserialize<ResolveResponseUid>(
                json
            );

            if (resolveResponse?.PlayerName != null && resolveResponse.Valid == 1)
            {
                _logger.LogDebug(
                    "Resolved player UID '{PlayerUid}' to name '{PlayerName}'",
                    playerUid,
                    resolveResponse.PlayerName
                );
                return resolveResponse.PlayerName;
            }

            _logger.LogWarning(
                "Failed to resolve player UID '{PlayerUid}': No name in response or invalid",
                playerUid
            );
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Failed to resolve player UID '{PlayerUid}': HTTP error",
                playerUid
            );
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve player UID '{PlayerUid}'", playerUid);
            return null;
        }
    }

    /// <summary>
    /// Response from auth server when resolving player name to UID.
    /// Matches the format from VintageStory's ResolveResponse class.
    /// </summary>
    private class ResolveResponse
    {
        [JsonPropertyName("playeruid")]
        public string? PlayerUid { get; set; }

        [JsonPropertyName("valid")]
        public int Valid { get; set; }
    }

    /// <summary>
    /// Response from auth server when resolving player UID to name.
    /// Matches the format from VintageStory's ResolveResponseUid class.
    /// </summary>
    private class ResolveResponseUid
    {
        [JsonPropertyName("playername")]
        public string? PlayerName { get; set; }

        [JsonPropertyName("valid")]
        public int Valid { get; set; }
    }
}
