using Granite.Common.Dto;
using Granite.Common.Dto.JsonApi;
using Granite.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Granite.Server.Controllers;

[Authorize(Roles = "Admin")]
[Route("api/admin/users")]
[ApiController]
public class UserAdminController : ControllerBase
{
    private readonly UserAdminService _userAdminService;
    private readonly ILogger<UserAdminController> _logger;

    public UserAdminController(UserAdminService userAdminService, ILogger<UserAdminController> logger)
    {
        _userAdminService = userAdminService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all registered users.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<JsonApiDocument<IList<UserDTO>>>> GetAllUsers()
    {
        var users = await _userAdminService.GetAllUsersAsync();
        return Ok(new JsonApiDocument<IList<UserDTO>>(users));
    }

    /// <summary>
    /// Approves a pending user, allowing them to log in.
    /// </summary>
    [HttpPost("{id}/approve")]
    public async Task<IActionResult> ApproveUser(string id)
    {
        var (success, error) = await _userAdminService.ApproveUserAsync(id);

        if (!success)
        {
            return error == "User not found"
                ? NotFound(new JsonApiDocument<UserDTO> { Errors = { new JsonApiError { Code = "NOT_FOUND", Message = error! } } })
                : StatusCode(StatusCodes.Status500InternalServerError, new JsonApiDocument<UserDTO> { Errors = { new JsonApiError { Code = "UPDATE_FAILED", Message = error! } } });
        }

        return NoContent();
    }

    /// <summary>
    /// Updates a user's email and roles.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<JsonApiDocument<UserDTO>>> UpdateUser(
        string id,
        [FromBody] UpdateUserDTO dto
    )
    {
        var (success, error, user) = await _userAdminService.UpdateUserAsync(id, dto);

        if (!success)
        {
            return error == "User not found"
                ? NotFound(new JsonApiDocument<UserDTO> { Errors = { new JsonApiError { Code = "NOT_FOUND", Message = error! } } })
                : StatusCode(StatusCodes.Status500InternalServerError, new JsonApiDocument<UserDTO> { Errors = { new JsonApiError { Code = "UPDATE_FAILED", Message = error! } } });
        }

        return Ok(new JsonApiDocument<UserDTO>(user!));
    }

    /// <summary>
    /// Resets a user's password.
    /// </summary>
    [HttpPost("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(string id, [FromBody] ResetPasswordDTO dto)
    {
        var (success, error) = await _userAdminService.ResetPasswordAsync(id, dto.NewPassword);

        if (!success)
        {
            return error == "User not found"
                ? NotFound(new JsonApiDocument<object> { Errors = { new JsonApiError { Code = "NOT_FOUND", Message = error! } } })
                : BadRequest(new JsonApiDocument<object> { Errors = { new JsonApiError { Code = "RESET_FAILED", Message = error! } } });
        }

        return NoContent();
    }

    /// <summary>
    /// Deletes (rejects) a user account.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var (success, error) = await _userAdminService.DeleteUserAsync(id);

        if (!success)
        {
            return error == "User not found"
                ? NotFound(new JsonApiDocument<UserDTO> { Errors = { new JsonApiError { Code = "NOT_FOUND", Message = error! } } })
                : StatusCode(StatusCodes.Status500InternalServerError, new JsonApiDocument<UserDTO> { Errors = { new JsonApiError { Code = "DELETE_FAILED", Message = error! } } });
        }

        return NoContent();
    }
}
