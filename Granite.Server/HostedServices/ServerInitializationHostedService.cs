using Granite.Server.Configuration;
using GraniteServer.Data;
using GraniteServer.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GraniteServer.Server.HostedServices;

public class ServerInitializationHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly GraniteServerOptions _options;
    private readonly ILogger<ServerInitializationHostedService> _logger;

    public ServerInitializationHostedService(
        IServiceProvider serviceProvider,
        IOptions<GraniteServerOptions> options,
        ILogger<ServerInitializationHostedService> logger
    )
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Initializing server configuration with ServerId: {ServerId}",
            _options.GraniteModServerId
        );

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GraniteDataContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        try
        {
            // Seed admin user
            await SeedAdminUserAsync(userManager, cancellationToken);

            // Check if server entity exists
            var serverEntity = await dbContext.Servers.FirstOrDefaultAsync(
                s => s.Id == _options.GraniteModServerId,
                cancellationToken
            );

            if (serverEntity == null)
            {
                // Create new server entity
                serverEntity = new ServerEntity
                {
                    Id = _options.GraniteModServerId,
                    Name = "Vintage Story Server",
                    Description = "Granite Server Instance",
                    AccessToken = _options.GraniteModToken!,
                    CreatedAt = DateTime.UtcNow,
                };

                dbContext.Servers.Add(serverEntity);
                await dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Created new server entity with ID: {ServerId}, Name: {ServerName}",
                    serverEntity.Id,
                    serverEntity.Name
                );
            }
            else
            {
                // Update access token if changed
                if (serverEntity.AccessToken != _options.GraniteModToken)
                {
                    _logger.LogInformation(
                        "Updating access token for server: {ServerId}",
                        serverEntity.Id
                    );
                    serverEntity.AccessToken = _options.GraniteModToken!;
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    _logger.LogInformation(
                        "Server entity already exists: {ServerId}, Name: {ServerName}",
                        serverEntity.Id,
                        serverEntity.Name
                    );
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to initialize server entity for ServerId: {ServerId}",
                _options.GraniteModServerId
            );
            throw;
        }
    }

    private async Task SeedAdminUserAsync(
        UserManager<ApplicationUser> userManager,
        CancellationToken cancellationToken
    )
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
            _logger.LogInformation(
                "Admin user '{Username}' already exists. Skipping seeding.",
                adminUsername
            );
            return;
        }

        var adminUser = new ApplicationUser
        {
            UserName = adminUsername,
            Email = null, // Email is optional
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);

        if (result.Succeeded)
        {
            _logger.LogInformation(
                "Admin user '{Username}' created successfully.",
                adminUsername
            );

            // Log a warning if using the default random password
            if (adminPassword.Length == 36 && Guid.TryParse(adminPassword, out _))
            {
                _logger.LogWarning(
                    "Admin user created with auto-generated password. Please change this password in production!"
                );
            }
        }
        else
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError(
                "Failed to create admin user '{Username}': {Errors}",
                adminUsername,
                errors
            );
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
