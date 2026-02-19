namespace Granite.Common.Dto;

/// <summary>
/// Represents a user account as returned by the admin API.
/// </summary>
public record UserDTO(
    string Id,
    string Username,
    string? Email,
    bool IsApproved,
    DateTime RegisteredAt,
    IList<string> Roles
);
