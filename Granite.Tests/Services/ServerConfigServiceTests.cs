using System;
using System.Threading.Tasks;
using FluentAssertions;
using Granite.Common.Dto;
using Granite.Server.Services;
using GraniteServer.Data;
using GraniteServer.Data.Entities;
using GraniteServer.Messaging;
using GraniteServer.Messaging.Commands;
using GraniteServer.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Granite.Tests.Services;

public class ServerConfigServiceTests
{
    private readonly GraniteDataContext _dataContext;
    private readonly PersistentMessageBusService _mockMessageBus;
    private readonly ServerConfigService _service;

    public ServerConfigServiceTests()
    {
        var options = new DbContextOptionsBuilder<GraniteDataContext>()
            .UseInMemoryDatabase($"ServerConfigServiceTests_{Guid.NewGuid()}")
            .Options;

        _dataContext = new GraniteDataContext(options);

        // Mock the dependencies for PersistentMessageBusService
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var logger = Substitute.For<ILogger<PersistentMessageBusService>>();
        var serviceLogger = Substitute.For<ILogger<ServerConfigService>>();

        // Use ForPartsOf with correct constructor arguments
        _mockMessageBus = Substitute.ForPartsOf<PersistentMessageBusService>(scopeFactory, logger);

        // Configure PublishCommandAsync to not call the base implementation
        _mockMessageBus.PublishCommandAsync(Arg.Any<CommandMessage>()).Returns(Guid.NewGuid());

        _service = new ServerConfigService(serviceLogger, _mockMessageBus, _dataContext);
    }

    [Fact]
    public async Task GetServerConfigAsync_ServerExists_ReturnsConfigWithServerName()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var server = new ServerEntity
        {
            Id = serverId,
            Name = "TestServer",
            AccessToken = "token",
            CreatedAt = DateTime.UtcNow,
        };

        _dataContext.Servers.Add(server);
        await _dataContext.SaveChangesAsync();

        // Act
        var result = await _service.GetServerConfigAsync(serverId);

        // Assert
        result.Should().NotBeNull();
        result!.ServerName.Should().Be("TestServer");
    }

    [Fact]
    public async Task GetServerConfigAsync_ServerDoesNotExist_ReturnsNull()
    {
        // Arrange
        var serverId = Guid.NewGuid();

        // Act
        var result = await _service.GetServerConfigAsync(serverId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SyncServerConfigAsync_PublishesCommandWithCorrectServerId()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        SyncServerConfigCommand? capturedCommand = null;

        _mockMessageBus
            .When(x => x.PublishCommandAsync(Arg.Any<SyncServerConfigCommand>()))
            .Do(callInfo => capturedCommand = callInfo.Arg<SyncServerConfigCommand>());

        // Act
        await _service.SyncServerConfigAsync(serverId);

        // Assert
        await _mockMessageBus.Received(1).PublishCommandAsync(Arg.Any<SyncServerConfigCommand>());
        capturedCommand.Should().NotBeNull();
        capturedCommand!.TargetServerId.Should().Be(serverId);
    }

    [Fact]
    public async Task UpdateServerConfigAsync_PublishesCommandWithCorrectServerIdAndConfig()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var config = new ServerConfigDTO
        {
            ServerName = "UpdatedServer",
            MaxClients = 50,
            AllowPvP = true,
        };

        UpdateServerConfigCommand? capturedCommand = null;

        _mockMessageBus
            .When(x => x.PublishCommandAsync(Arg.Any<UpdateServerConfigCommand>()))
            .Do(callInfo => capturedCommand = callInfo.Arg<UpdateServerConfigCommand>());

        // Act
        await _service.UpdateServerConfigAsync(serverId, config);

        // Assert
        await _mockMessageBus.Received(1).PublishCommandAsync(Arg.Any<UpdateServerConfigCommand>());
        capturedCommand.Should().NotBeNull();
        capturedCommand!.TargetServerId.Should().Be(serverId);
        capturedCommand.Data.Config.Should().BeSameAs(config);
        capturedCommand.Data.Config.ServerName.Should().Be("UpdatedServer");
        capturedCommand.Data.Config.MaxClients.Should().Be(50);
        capturedCommand.Data.Config.AllowPvP.Should().Be(true);
    }

    [Fact]
    public async Task UpdateServerConfigAsync_PartialConfig_OnlyUpdatesSpecifiedProperties()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var config = new ServerConfigDTO
        {
            ServerName = "PartialUpdate",
            MaxClients = null, // Not updated
            AllowPvP = null, // Not updated
        };

        UpdateServerConfigCommand? capturedCommand = null;

        _mockMessageBus
            .When(x => x.PublishCommandAsync(Arg.Any<UpdateServerConfigCommand>()))
            .Do(callInfo => capturedCommand = callInfo.Arg<UpdateServerConfigCommand>());

        // Act
        await _service.UpdateServerConfigAsync(serverId, config);

        // Assert
        capturedCommand.Should().NotBeNull();
        capturedCommand!.Data.Config.ServerName.Should().Be("PartialUpdate");
        capturedCommand.Data.Config.MaxClients.Should().BeNull();
        capturedCommand.Data.Config.AllowPvP.Should().BeNull();
    }
}
