using System.ComponentModel.DataAnnotations;

namespace Granite.Common.Dto;

/// <summary>
/// Request DTO for updating a user's profile by an admin.
/// </summary>
public record UpdateUserDTO
{
    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; init; }

    /// <summary>
    /// Full list of roles to assign. Replaces existing roles.
    /// </summary>
    public IList<string> Roles { get; init; } = new List<string>();
}
