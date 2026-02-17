using System.ComponentModel.DataAnnotations;

namespace Granite.Common.Dto;

/// <summary>
/// Request DTO for user registration
/// </summary>
public record RegisterDTO
{
    [Required]
    [StringLength(256, MinimumLength = 3)]
    public string Username { get; init; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;

    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; init; }
}
