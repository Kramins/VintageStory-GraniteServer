using Granite.Common.Dto;
using Granite.Common.Dto.JsonApi;

namespace Granite.Web.Client.Services.Api;

public interface IUserAdminApiClient
{
    Task<JsonApiDocument<IList<UserDTO>>> GetAllUsersAsync();
    Task ApproveUserAsync(string id);
    Task<JsonApiDocument<UserDTO>> UpdateUserAsync(string id, UpdateUserDTO dto);
    Task ResetPasswordAsync(string id, string newPassword);
    Task DeleteUserAsync(string id);
}
