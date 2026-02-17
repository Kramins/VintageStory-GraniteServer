using Granite.Common.Dto;
using Granite.Common.Dto.JsonApi;

namespace Granite.Web.Client.Services.Api;

/// <summary>
/// Interface for Authentication API client.
/// </summary>
public interface IAuthApiClient
{
    /// <summary>
    /// Logs in a user with username and password.
    /// </summary>
    Task<JsonApiDocument<TokenDTO>> LoginAsync(string username, string password);

    /// <summary>
    /// Logs in using OAuth/external provider.
    /// </summary>
    Task<JsonApiDocument<TokenDTO>> OAuthLoginAsync(string provider, string code);

    /// <summary>
    /// Refreshes an expired access token.
    /// </summary>
    Task<JsonApiDocument<TokenDTO>> RefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Validates the current access token.
    /// </summary>
    Task<JsonApiDocument<bool>> ValidateTokenAsync(string token);

    /// <summary>
    /// Logs out the current user.
    /// </summary>
    Task<JsonApiDocument<object>> LogoutAsync();

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    Task<JsonApiDocument<object>> RegisterAsync(string username, string password, string? email = null);

    /// <summary>
    /// Gets the current authentication settings.
    /// </summary>
    Task<JsonApiDocument<AuthSettingsDTO>> GetAuthSettingsAsync();
}
