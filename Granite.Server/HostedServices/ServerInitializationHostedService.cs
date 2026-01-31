using Granite.Server.Configuration;
using GraniteServer.Data;
using GraniteServer.Data.Entities;
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

        try
        {
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

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
