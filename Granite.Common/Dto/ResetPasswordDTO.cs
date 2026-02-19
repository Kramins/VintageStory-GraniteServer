using System.ComponentModel.DataAnnotations;

namespace Granite.Common.Dto;

/// <summary>
/// Request DTO for an admin resetting a user's password.
/// </summary>
public record ResetPasswordDTO
{
    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string NewPassword { get; init; } = string.Empty;
}
