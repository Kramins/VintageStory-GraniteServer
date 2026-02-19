using Granite.Common.Dto;
using Granite.Common.Dto.JsonApi;

namespace Granite.Web.Client.Services.Api;

/// <summary>
/// HTTP client for the admin user management API.
/// </summary>
public class UserAdminApiClient : BaseApiClient, IUserAdminApiClient
{
    private const string BasePath = "/api/admin/users";

    public UserAdminApiClient(IHttpClientFactory httpClientFactory, ILogger<UserAdminApiClient> logger)
        : base(httpClientFactory, logger)
    {
    }

    public async Task<JsonApiDocument<IList<UserDTO>>> GetAllUsersAsync()
    {
        try
        {
            return await GetAsync<IList<UserDTO>>(BasePath);
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to fetch users");
            throw;
        }
    }

    public async Task ApproveUserAsync(string id)
    {
        try
        {
            await PostAsync<object>($"{BasePath}/{id}/approve", null);
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to approve user {Id}", id);
            throw;
        }
    }

    public async Task<JsonApiDocument<UserDTO>> UpdateUserAsync(string id, UpdateUserDTO dto)
    {
        try
        {
            return await PutAsync<UserDTO>($"{BasePath}/{id}", dto);
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to update user {Id}", id);
            throw;
        }
    }

    public async Task ResetPasswordAsync(string id, string newPassword)
    {
        try
        {
            await PostAsync<object>($"{BasePath}/{id}/reset-password", new ResetPasswordDTO { NewPassword = newPassword });
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to reset password for user {Id}", id);
            throw;
        }
    }

    public async Task DeleteUserAsync(string id)
    {
        try
        {
            await DeleteAsync($"{BasePath}/{id}");
        }
        catch (ApiException ex)
        {
            Logger.LogError(ex, "Failed to delete user {Id}", id);
            throw;
        }
    }
}
