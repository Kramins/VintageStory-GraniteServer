using Microsoft.AspNetCore.Identity;

namespace GraniteServer.Data.Entities;

/// <summary>
/// Represents a user account in the Granite Server system.
/// Extends ASP.NET Core Identity's IdentityUser to provide authentication and authorization.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Whether an admin has approved this account. Accounts pending approval cannot log in.
    /// Admin-seeded accounts are automatically approved.
    /// </summary>
    public bool IsApproved { get; set; } = false;

    /// <summary>
    /// UTC timestamp of when the user registered.
    /// </summary>
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}
