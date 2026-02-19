using Granite.Server.Configuration;
using GraniteServer.Data;
using GraniteServer.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GraniteServer.Server.HostedServices;

/// <summary>
/// Ensures the Vintage Story game server entity exists in the database on startup,
/// and keeps the mod access token in sync with configuration.
/// </summary>
public class VintageStoryServerInitializationHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly GraniteServerOptions _options;
    private readonly ILogger<VintageStoryServerInitializationHostedService> _logger;

    public VintageStoryServerInitializationHostedService(
        IServiceProvider serviceProvider,
        IOptions<GraniteServerOptions> options,
        ILogger<VintageStoryServerInitializationHostedService> logger
    )
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Initializing Vintage Story server configuration with ServerId: {ServerId}",
            _options.GraniteModServerId
        );

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GraniteDataContext>();

        try
        {
            var serverEntity = await dbContext.Servers.FirstOrDefaultAsync(
                s => s.Id == _options.GraniteModServerId,
                cancellationToken
            );

            if (serverEntity == null)
            {
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

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
