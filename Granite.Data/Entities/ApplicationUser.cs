using Microsoft.AspNetCore.Identity;

namespace GraniteServer.Data.Entities;

/// <summary>
/// Represents a user account in the Granite Server system.
/// Extends ASP.NET Core Identity's IdentityUser to provide authentication and authorization.
/// </summary>
public class ApplicationUser : IdentityUser
{
    // Additional properties can be added here in the future
    // Identity provides: UserName, Email, PasswordHash, PhoneNumber, etc.
}
