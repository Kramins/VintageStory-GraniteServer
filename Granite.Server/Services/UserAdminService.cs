using Granite.Common.Dto;
using GraniteServer.Data.Entities;
using Microsoft.AspNetCore.Identity;

namespace Granite.Server.Services;

/// <summary>
/// Service for admin user management operations.
/// </summary>
public class UserAdminService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<UserAdminService> _logger;

    public UserAdminService(
        UserManager<ApplicationUser> userManager,
        ILogger<UserAdminService> logger
    )
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<IList<UserDTO>> GetAllUsersAsync()
    {
        var users = _userManager.Users.ToList();
        var result = new List<UserDTO>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(MapToDto(user, roles));
        }

        return result;
    }

    public async Task<UserDTO?> GetUserByIdAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return null;

        var roles = await _userManager.GetRolesAsync(user);
        return MapToDto(user, roles);
    }

    public async Task<(bool Success, string? Error)> ApproveUserAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return (false, "User not found");

        user.IsApproved = true;
        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
            return (false, string.Join(", ", result.Errors.Select(e => e.Description)));

        _logger.LogInformation("User '{Username}' approved", user.UserName);
        return (true, null);
    }

    public async Task<(bool Success, string? Error, UserDTO? User)> UpdateUserAsync(
        string id,
        UpdateUserDTO dto
    )
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return (false, "User not found", null);

        user.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return (false, string.Join(", ", updateResult.Errors.Select(e => e.Description)), null);

        // Sync roles: remove any not in the new list, add any missing
        var currentRoles = await _userManager.GetRolesAsync(user);
        var toRemove = currentRoles.Except(dto.Roles).ToList();
        var toAdd = dto.Roles.Except(currentRoles).ToList();

        if (toRemove.Count > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, toRemove);
            if (!removeResult.Succeeded)
                return (false, string.Join(", ", removeResult.Errors.Select(e => e.Description)), null);
        }

        if (toAdd.Count > 0)
        {
            var addResult = await _userManager.AddToRolesAsync(user, toAdd);
            if (!addResult.Succeeded)
                return (false, string.Join(", ", addResult.Errors.Select(e => e.Description)), null);
        }

        _logger.LogInformation("User '{Username}' updated", user.UserName);
        var updatedRoles = await _userManager.GetRolesAsync(user);
        return (true, null, MapToDto(user, updatedRoles));
    }

    public async Task<(bool Success, string? Error)> ResetPasswordAsync(string id, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return (false, "User not found");

        var removeResult = await _userManager.RemovePasswordAsync(user);
        if (!removeResult.Succeeded)
            return (false, string.Join(", ", removeResult.Errors.Select(e => e.Description)));

        var addResult = await _userManager.AddPasswordAsync(user, newPassword);
        if (!addResult.Succeeded)
            return (false, string.Join(", ", addResult.Errors.Select(e => e.Description)));

        _logger.LogInformation("Password reset for user '{Username}'", user.UserName);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteUserAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return (false, "User not found");

        var result = await _userManager.DeleteAsync(user);

        if (!result.Succeeded)
            return (false, string.Join(", ", result.Errors.Select(e => e.Description)));

        _logger.LogInformation("User '{Username}' deleted", user.UserName);
        return (true, null);
    }

    private static UserDTO MapToDto(ApplicationUser user, IList<string> roles) =>
        new UserDTO(
            user.Id,
            user.UserName!,
            user.Email,
            user.IsApproved,
            user.RegisteredAt,
            roles
        );
}
