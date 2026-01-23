using System;
using System.Threading.Tasks;
using FluentAssertions;
using Granite.Common.Dto;
using GraniteServer.Data;
using GraniteServer.Data.Entities;
using GraniteServer.Messaging.Events;
using GraniteServer.Messaging.Handlers.Events;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Granite.Tests.Handlers;

public class ServerConfigEventHandlerTests
{
    private readonly GraniteDataContext _dataContext;
    private readonly ServerConfigEventHandler _handler;

    public ServerConfigEventHandlerTests()
    {
        var options = new DbContextOptionsBuilder<GraniteDataContext>()
            .UseInMemoryDatabase($"ServerConfigTests_{Guid.NewGuid()}")
            .Options;

        _dataContext = new GraniteDataContext(options);
        _handler = new ServerConfigEventHandler(_dataContext);
    }

    [Fact]
    public async Task Handle_ValidConfigEvent_UpdatesServerName()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var server = new ServerEntity
        {
            Id = serverId,
            Name = "OldServerName",
            AccessToken = "test-token",
            CreatedAt = DateTime.UtcNow,
        };

        _dataContext.Servers.Add(server);
        await _dataContext.SaveChangesAsync();

        var @event = new ServerConfigSyncedEvent
        {
            OriginServerId = serverId,
            Data = new ServerConfigSyncedEventData
            {
                Config = new ServerConfigDTO
                {
                    ServerName = "NewServerName",
                    Port = 12345,
                    MaxClients = 32,
                },
            },
        };

        // Act
        await ((IEventHandler<ServerConfigSyncedEvent>)_handler).Handle(@event);

        // Assert
        var updated = await _dataContext.Servers.FindAsync(serverId);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("NewServerName");
    }

    [Fact]
    public async Task Handle_ServerNotFound_DoesNotThrow()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var @event = new ServerConfigSyncedEvent
        {
            OriginServerId = serverId,
            Data = new ServerConfigSyncedEventData
            {
                Config = new ServerConfigDTO { ServerName = "TestServer" },
            },
        };

        // Act
        Func<Task> act = async () =>
            await ((IEventHandler<ServerConfigSyncedEvent>)_handler).Handle(@event);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_NullOrEmptyServerName_DoesNotUpdateServerName()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var server = new ServerEntity
        {
            Id = serverId,
            Name = "OriginalName",
            AccessToken = "test-token",
            CreatedAt = DateTime.UtcNow,
        };

        _dataContext.Servers.Add(server);
        await _dataContext.SaveChangesAsync();

        var @event = new ServerConfigSyncedEvent
        {
            OriginServerId = serverId,
            Data = new ServerConfigSyncedEventData
            {
                Config = new ServerConfigDTO { ServerName = null, Port = 12345 },
            },
        };

        // Act
        await ((IEventHandler<ServerConfigSyncedEvent>)_handler).Handle(@event);

        // Assert
        var updated = await _dataContext.Servers.FindAsync(serverId);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("OriginalName");
    }

    [Fact]
    public async Task Handle_SameServerName_DoesNotUpdate()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var server = new ServerEntity
        {
            Id = serverId,
            Name = "SameName",
            AccessToken = "test-token",
            CreatedAt = DateTime.UtcNow,
        };

        _dataContext.Servers.Add(server);
        await _dataContext.SaveChangesAsync();

        var @event = new ServerConfigSyncedEvent
        {
            OriginServerId = serverId,
            Data = new ServerConfigSyncedEventData
            {
                Config = new ServerConfigDTO { ServerName = "SameName" },
            },
        };

        // Act
        await ((IEventHandler<ServerConfigSyncedEvent>)_handler).Handle(@event);

        // Assert
        var updated = await _dataContext.Servers.FindAsync(serverId);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("SameName");
    }
}
