using System.Text.Json;
using FluentAssertions;
using Granite.Server.Hubs;
using Granite.Server.Services;
using GraniteServer.Messaging;
using GraniteServer.Messaging.Events;
using GraniteServer.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Granite.Tests.Hubs;

public class ModHubTests
{
    private readonly ILogger<ModHub> _mockLogger;
    private readonly PersistentMessageBusService _mockMessageBus;
    private readonly ServersService _mockServersService;
    private readonly ModHub _hub;

    public ModHubTests()
    {
        _mockLogger = Substitute.For<ILogger<ModHub>>();
        _mockMessageBus = Substitute.For<PersistentMessageBusService>();
        _mockServersService = Substitute.For<ServersService>();

        _hub = new ModHub(_mockMessageBus, _mockLogger, _mockServersService);
    }

    [Fact]
    public void DeserializeEvent_ValidPlayerJoinedEvent_ReturnsDeserializedMessage()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var eventPayload = new
        {
            messageType = "PlayerJoinedEvent",
            originServerId = serverId.ToString(),
            targetServerId = "87654321-4321-4321-4321-210987654321",
            timestamp = DateTime.UtcNow,
            data = new { playerId = Guid.NewGuid().ToString(), name = "TestPlayer" },
        };
        var json = JsonSerializer.Serialize(eventPayload);
        var jsonElement = JsonDocument.Parse(json).RootElement;
        var messageType = typeof(PlayerJoinedEvent);

        // Act
        var result = _hub.DeserializeEvent(jsonElement, messageType);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<PlayerJoinedEvent>();
        result.OriginServerId.Should().Be(serverId);
    }

    [Fact]
    public void DeserializeEvent_WithCamelCaseProperties_DeserializesSuccessfully()
    {
        // Arrange
        var serverId = Guid.NewGuid();
        var eventPayload = new
        {
            messageType = "PlayerJoinedEvent",
            originServerId = serverId.ToString(),
            targetServerId = Guid.NewGuid().ToString(),
            data = new { playerId = Guid.NewGuid().ToString(), name = "Player" },
        };
        var json = JsonSerializer.Serialize(eventPayload);
        var jsonElement = JsonDocument.Parse(json).RootElement;
        var messageType = typeof(PlayerJoinedEvent);

        // Act
        var result = _hub.DeserializeEvent(jsonElement, messageType);

        // Assert
        result.Should().NotBeNull();
        result.OriginServerId.Should().Be(serverId);
    }

    [Fact]
    public void DeserializeEvent_MissingOriginServerId_ThrowsInvalidOperationException()
    {
        // Arrange
        var eventPayload = new
        {
            messageType = "PlayerJoinedEvent",
            targetServerId = Guid.NewGuid().ToString(),
            data = new { playerId = Guid.NewGuid().ToString(), name = "Player" },
        };
        var json = JsonSerializer.Serialize(eventPayload);
        var jsonElement = JsonDocument.Parse(json).RootElement;
        var messageType = typeof(PlayerJoinedEvent);

        // Act & Assert
        var action = () => _hub.DeserializeEvent(jsonElement, messageType);
        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void DeserializeEvent_MissingTargetServerId_ThrowsInvalidOperationException()
    {
        // Arrange
        var eventPayload = new
        {
            messageType = "PlayerJoinedEvent",
            originServerId = Guid.NewGuid().ToString(),
            data = new { playerId = Guid.NewGuid().ToString(), name = "Player" },
        };
        var json = JsonSerializer.Serialize(eventPayload);
        var jsonElement = JsonDocument.Parse(json).RootElement;
        var messageType = typeof(PlayerJoinedEvent);

        // Act & Assert
        var action = () => _hub.DeserializeEvent(jsonElement, messageType);
        action.Should().Throw<InvalidOperationException>();
    }
}
