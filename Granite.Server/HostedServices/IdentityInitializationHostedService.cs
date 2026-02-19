using Granite.Server.Configuration;
using GraniteServer.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace GraniteServer.Server.HostedServices;

/// <summary>
/// Handles all ASP.NET Identity lifecycle initialization on startup:
/// ensures required roles exist and seeds the configured admin user.
/// </summary>
public class IdentityInitializationHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly GraniteServerOptions _options;
    private readonly ILogger<IdentityInitializationHostedService> _logger;

    public IdentityInitializationHostedService(
        IServiceProvider serviceProvider,
        IOptions<GraniteServerOptions> options,
        ILogger<IdentityInitializationHostedService> logger
    )
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await EnsureRolesAsync(roleManager);
        await SeedAdminUserAsync(userManager);
    }

    private async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in new[] { "Admin", "User" })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                if (result.Succeeded)
                    _logger.LogInformation("Created Identity role '{Role}'", role);
                else
                    _logger.LogError(
                        "Failed to create Identity role '{Role}': {Errors}",
                        role,
                        string.Join(", ", result.Errors.Select(e => e.Description))
                    );
            }
        }
    }

    private async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager)
    {
        var adminUsername = _options.Username;
        var adminPassword = _options.Password;

        if (string.IsNullOrWhiteSpace(adminUsername) || string.IsNullOrWhiteSpace(adminPassword))
        {
            _logger.LogWarning(
                "Admin username or password not configured. Skipping admin user seeding."
            );
            return;
        }

        var existingAdmin = await userManager.FindByNameAsync(adminUsername);
        if (existingAdmin != null)
        {
            // Ensure existing admin has the Admin role and is approved
            if (!await userManager.IsInRoleAsync(existingAdmin, "Admin"))
            {
                await userManager.AddToRoleAsync(existingAdmin, "Admin");
                _logger.LogInformation(
                    "Assigned Admin role to existing user '{Username}'",
                    adminUsername
                );
            }

            if (!existingAdmin.IsApproved)
            {
                existingAdmin.IsApproved = true;
                await userManager.UpdateAsync(existingAdmin);
                _logger.LogInformation(
                    "Approved existing admin user '{Username}'",
                    adminUsername
                );
            }

            return;
        }

        var adminUser = new ApplicationUser
        {
            UserName = adminUsername,
            Email = null,
            IsApproved = true,
            RegisteredAt = DateTime.UtcNow,
        };

        var createResult = await userManager.CreateAsync(adminUser, adminPassword);

        if (!createResult.Succeeded)
        {
            var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            _logger.LogError(
                "Failed to create admin user '{Username}': {Errors}",
                adminUsername,
                errors
            );
            return;
        }

        await userManager.AddToRoleAsync(adminUser, "Admin");
        _logger.LogInformation("Admin user '{Username}' created and assigned Admin role", adminUsername);

        if (adminPassword.Length == 36 && Guid.TryParse(adminPassword, out _))
        {
            _logger.LogWarning(
                "Admin user created with auto-generated password. Change this in production!"
            );
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
