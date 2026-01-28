using Granite.Common.Dto;
using Granite.Common.Dto.JsonApi;

namespace Granite.Web.Client.Services.Api;

/// <summary>
/// HTTP client for Authentication API.
/// </summary>
public class AuthApiClient : BaseApiClient, IAuthApiClient
{
    private const string BasePath = "/api/auth";

    public AuthApiClient(HttpClient httpClient, ILogger<AuthApiClient> logger)
        : base(httpClient, logger)
    {
    }

    public async Task<JsonApiDocument<TokenDTO>> LoginAsync(string username, string password)
    {
        try
        {
            var request = new BasicAuthCredentialsDTO { Username = username, Password = password };
            return await PostAsync<TokenDTO>($"{BasePath}/login", request);
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Login failed for user {Username}", username);
            throw;
        }
    }

    public async Task<JsonApiDocument<TokenDTO>> OAuthLoginAsync(string provider, string code)
    {
        try
        {
            var request = new { provider, code };
            return await PostAsync<TokenDTO>($"{BasePath}/oauth/callback", request);
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "OAuth login failed for provider {Provider}", provider);
            throw;
        }
    }

    public async Task<JsonApiDocument<TokenDTO>> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var request = new { refreshToken };
            return await PostAsync<TokenDTO>($"{BasePath}/refresh", request);
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to refresh token");
            throw;
        }
    }

    public async Task<JsonApiDocument<bool>> ValidateTokenAsync(string token)
    {
        try
        {
            var request = new { token };
            return await PostAsync<bool>($"{BasePath}/validate", request);
        }
        catch (ApiException)
        {
            return new JsonApiDocument<bool> { Data = false };
        }
    }

    public async Task<JsonApiDocument<object>> LogoutAsync()
    {
        try
        {
            return await PostAsync<object>($"{BasePath}/logout", null);
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Logout failed");
            throw;
        }
    }

    public async Task<JsonApiDocument<AuthSettingsDTO>> GetAuthSettingsAsync()
    {
        try
        {
            return await GetAsync<AuthSettingsDTO>($"{BasePath}/settings");
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to fetch auth settings");
            throw;
        }
    }
}
